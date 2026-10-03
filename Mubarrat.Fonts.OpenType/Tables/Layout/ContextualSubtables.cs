using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.OpenType.Binary;

namespace Mubarrat.Fonts.OpenType.Tables.Layout;

/// <summary>A SequenceLookupRecord: a nested lookup to apply at a specific position in the input sequence. Shared by every contextual and chained contextual subtable format.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Blittable, size 4, no padding. Fixed-layout arrays of these are read in one call via <see cref="Cursor.ReadBigEndianStructArray{T}(int)"/>.</description></item>
/// <item><description>Each record pairs a position in the matched input sequence with an index into the enclosing LookupList; the referenced lookup is applied as if invoked directly at that position.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequencelookuprecord">SequenceLookupRecord</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SequenceRule"/>
/// <seealso cref="SequenceContextFormat3"/>
/// <seealso cref="ChainedSequenceRule"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequencelookuprecord">OpenType specification: SequenceLookupRecord</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct SequenceLookupRecord : IBigEndianStruct<SequenceLookupRecord>
{
    /// <summary>Gets the zero-based index into the input glyph sequence.</summary>
    /// <value>The position within the matched input sequence at which the referenced lookup applies. The index is measured from the first glyph of the input sequence; indices into backtrack and lookahead sequences are not used here.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequencelookuprecord"><c>sequenceIndex</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="LookupListIndex"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequencelookuprecord">OpenType specification: <c>sequenceIndex</c></seealso>
    public ushort SequenceIndex;

    /// <summary>Gets the zero-based index into the LookupList.</summary>
    /// <value>The index of the lookup to apply, within the enclosing LookupList of the table that owns this subtable.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequencelookuprecord"><c>lookupListIndex</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="SequenceIndex"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequencelookuprecord">OpenType specification: <c>lookupListIndex</c></seealso>
    public ushort LookupListIndex;

    /// <inheritdoc/>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new record with both fields reversed.</returns>
    /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequencelookuprecord">OpenType specification: SequenceLookupRecord</seealso>
    public static SequenceLookupRecord ReverseEndianness(SequenceLookupRecord v) => new()
    {
        SequenceIndex = BinaryPrimitives.ReverseEndianness(v.SequenceIndex),
        LookupListIndex = BinaryPrimitives.ReverseEndianness(v.LookupListIndex),
    };
}

/// <summary>Base class for the six contextual lookup subtable formats.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>A contextual lookup matches an input glyph sequence and, on a match, applies one or more nested lookups at specified positions in the sequence.</description></item>
/// <item><description>Formats 1 (simple glyphs), 2 (class-based), and 3 (coverage-based) are non-chained. Formats 1–3 of <see cref="ChainedSequenceContext"/> add backtrack and lookahead sequences.</description></item>
/// <item><description>The formats trade off expressiveness against table size: format 1 is fully explicit, format 2 shares class definitions across rules, format 3 lets each position use its own coverage table.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-1-simple-glyph-contexts">Sequence Context subtables</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SequenceContextFormat1"/>
/// <seealso cref="SequenceContextFormat2"/>
/// <seealso cref="SequenceContextFormat3"/>
/// <seealso cref="ChainedSequenceContext"/>
/// <seealso cref="SequenceLookupRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-1-simple-glyph-contexts">OpenType specification: Sequence Context subtables</seealso>
public abstract record SequenceContext : IRecord<SequenceContext>, IBaseRecord<SequenceContext>
{
    /// <summary>Gets the subtable format number (1, 2, or 3).</summary>
    /// <value>The format discriminant from the first <c>uint16</c> of the subtable.</value>
    /// <seealso cref="SequenceContextFormat1"/>
    /// <seealso cref="SequenceContextFormat2"/>
    /// <seealso cref="SequenceContextFormat3"/>
    public ushort Format { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable.</param>
    /// <param name="context">Context forwarded to the format-specific parser; usually unused.</param>
    /// <returns>The format-specific subtable.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1, 2, or 3.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-1-simple-glyph-contexts">Sequence Context subtables</see> in the OpenType specification.</remarks>
    /// <seealso cref="IBaseRecord{TBase}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-1-simple-glyph-contexts">OpenType specification: Sequence Context subtables</seealso>
    static SequenceContext IRecord<SequenceContext>.Parse(ref Cursor cursor, object? context)
    {
        ushort format = cursor.ReadUInt16();
        return format switch
        {
            1 => IBaseRecord<SequenceContext>.Parse<SequenceContextFormat1>(ref cursor, context),
            2 => IBaseRecord<SequenceContext>.Parse<SequenceContextFormat2>(ref cursor, context),
            3 => IBaseRecord<SequenceContext>.Parse<SequenceContextFormat3>(ref cursor, context),
            _ => throw new InvalidDataException($"SequenceContext format {format} is not defined."),
        };
    }
}

// ═══════════════════════════ FORMAT 1: SIMPLE GLYPH CONTEXTS ═══════════════════════════

/// <summary>Sequence context format 1: input sequences defined by specific glyph IDs. One rule set per initial glyph listed in the Coverage table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The Coverage table identifies the glyphs that can begin an input sequence; a rule set is looked up by coverage index.</description></item>
/// <item><description>Each <see cref="SequenceRule"/> in a rule set lists the remaining glyphs of the input sequence and the nested lookups to apply on a match.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-1-simple-glyph-contexts">Sequence Context Format 1</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SequenceContext"/>
/// <seealso cref="SequenceRuleSet"/>
/// <seealso cref="SequenceRule"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-1-simple-glyph-contexts">OpenType specification: Sequence Context Format 1</seealso>
public sealed record SequenceContextFormat1 : SequenceContext, IDerivedRecord<SequenceContext, SequenceContextFormat1>
{
    /// <summary>Gets the coverage table listing initial input glyphs.</summary>
    /// <value>The <see cref="Coverage"/> that identifies glyphs eligible to start a matched input sequence.</value>
    /// <seealso cref="RuleSets"/>
    /// <seealso cref="Coverage"/>
    public Coverage Coverage { get; init; } = null!;

    /// <summary>Gets the rule sets, in Coverage Index order. Entries are <c>null</c> when the corresponding offset is NULL.</summary>
    /// <value>An array whose length equals the coverage table's glyph count. Entry <c>i</c> is the rule set for the glyph at coverage index <c>i</c>, or <c>null</c> when no rules apply to that glyph.</value>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="SequenceRuleSet"/>
    public IReadOnlyList<SequenceRuleSet?> RuleSets { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-1 body (after the format discriminant).</param>
    /// <param name="context">Forwarded to the subtable parsers.</param>
    /// <returns>The parsed format-1 subtable.</returns>
    /// <exception cref="EndOfStreamException">The header, coverage, or rule-set array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-1-simple-glyph-contexts">Sequence Context Format 1</see> in the OpenType specification.</remarks>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="SequenceRuleSet"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-1-simple-glyph-contexts">OpenType specification: Sequence Context Format 1</seealso>
    static SequenceContextFormat1 IDerivedRecord<SequenceContext, SequenceContextFormat1>.Parse(ref Cursor cursor, object? context)
    {
        Source source = cursor.Source;
        return new()
        {
            Format = 1,
            Coverage = source.ParseRecordAt<Coverage>(cursor.ReadOffset16(), context),
            RuleSets = cursor.ReadOffset16ArrayInterpret(cursor.ReadUInt16(),
                off => off != 0 ? source.ParseRecordAt<SequenceRuleSet>(off, context) : null),
        };
    }
}

/// <summary>A set of <see cref="SequenceRule"/> tables, all beginning with the same glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Rules within a set are evaluated in declaration order; the first one whose remaining glyphs match the input is used.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-1-simple-glyph-contexts">SequenceRuleSet</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SequenceContextFormat1"/>
/// <seealso cref="SequenceRule"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-1-simple-glyph-contexts">OpenType specification: SequenceRuleSet</seealso>
public sealed record SequenceRuleSet : IRecord<SequenceRuleSet>
{
    /// <summary>Gets the rules, evaluated in declaration order; the first match is used.</summary>
    /// <value>The ordered list of <see cref="SequenceRule"/> entries. All rules in a set begin with the same glyph, but their remaining input sequences differ.</value>
    /// <seealso cref="SequenceRule"/>
    public IReadOnlyList<SequenceRule> Rules { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the rule set.</param>
    /// <param name="context">Forwarded to the rule parsers.</param>
    /// <returns>The parsed rule set.</returns>
    /// <exception cref="EndOfStreamException">The count or rule array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-1-simple-glyph-contexts">SequenceRuleSet</see> in the OpenType specification.</remarks>
    /// <seealso cref="SequenceRule"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-1-simple-glyph-contexts">OpenType specification: SequenceRuleSet</seealso>
    static SequenceRuleSet IRecord<SequenceRuleSet>.Parse(ref Cursor cursor, object? context) =>
        new() { Rules = cursor.ReadOffset16ArrayPeekRecord<SequenceRule>(cursor.ReadUInt16(), context) };
}

/// <summary>A single input sequence rule: remaining glyph IDs plus the actions to apply.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The initial glyph of the input sequence is implicit through the enclosing coverage index; only the remaining glyphs are stored here.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-1-simple-glyph-contexts">SequenceRule</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SequenceRuleSet"/>
/// <seealso cref="SequenceLookupRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-1-simple-glyph-contexts">OpenType specification: SequenceRule</seealso>
public sealed record SequenceRule : IRecord<SequenceRule>
{
    /// <summary>Gets the remaining glyph IDs (input positions 1..n). The initial glyph is matched through the parent Coverage table.</summary>
    /// <value>The glyph IDs that must follow the initial glyph for the rule to match. Length is <c>glyphCount - 1</c>.</value>
    /// <seealso cref="LookupRecords"/>
    /// <seealso cref="Coverage"/>
    public IReadOnlyList<ushort> InputSequence { get; init; } = [];

    /// <summary>Gets the nested lookups to apply, in application order.</summary>
    /// <value>The ordered list of <see cref="SequenceLookupRecord"/> entries. Each entry specifies a position in the matched sequence and a lookup to invoke there.</value>
    /// <seealso cref="InputSequence"/>
    /// <seealso cref="SequenceLookupRecord"/>
    public IReadOnlyList<SequenceLookupRecord> LookupRecords { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the rule.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed rule.</returns>
    /// <exception cref="EndOfStreamException">The header, glyph array, or lookup array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-1-simple-glyph-contexts">SequenceRule</see> in the OpenType specification.</remarks>
    /// <seealso cref="InputSequence"/>
    /// <seealso cref="LookupRecords"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-1-simple-glyph-contexts">OpenType specification: SequenceRule</seealso>
    static SequenceRule IRecord<SequenceRule>.Parse(ref Cursor cursor, object? context)
    {
        int glyphCount = cursor.ReadUInt16(), lookupCount = cursor.ReadUInt16();
        return new()
        {
            InputSequence = cursor.ReadUInt16Array(glyphCount - 1),
            LookupRecords = cursor.ReadBigEndianStructArray<SequenceLookupRecord>(lookupCount),
        };
    }
}

// ═══════════════════════════ FORMAT 2: CLASS-BASED GLYPH CONTEXTS ═══════════════════════════

/// <summary>Sequence context format 2: input sequence patterns defined as sequences of class values.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The <see cref="Coverage"/> identifies glyphs that can start a matched sequence; the <see cref="ClassDef"/> assigns a class value to every glyph.</description></item>
/// <item><description>Rule sets are indexed by the class value of the initial glyph; each rule then matches class values at the remaining positions.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-2-class-based-glyph-contexts">Sequence Context Format 2</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SequenceContext"/>
/// <seealso cref="ClassSequenceRuleSet"/>
/// <seealso cref="ClassSequenceRule"/>
/// <seealso cref="ClassDef"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-2-class-based-glyph-contexts">OpenType specification: Sequence Context Format 2</seealso>
public sealed record SequenceContextFormat2 : SequenceContext, IDerivedRecord<SequenceContext, SequenceContextFormat2>
{
    /// <summary>Gets the coverage table listing glyphs that can start a matched sequence.</summary>
    /// <value>The <see cref="Coverage"/> that identifies glyphs eligible to begin a matched sequence.</value>
    /// <seealso cref="ClassDef"/>
    /// <seealso cref="ClassRuleSets"/>
    /// <seealso cref="Coverage"/>
    public Coverage Coverage { get; init; } = null!;

    /// <summary>Gets the class definition used to assign classes to glyphs.</summary>
    /// <value>The <see cref="ClassDef"/> that maps each glyph to its class value for the input sequence positions.</value>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="ClassRuleSets"/>
    /// <seealso cref="ClassDef"/>
    public ClassDef ClassDef { get; init; } = null!;

    /// <summary>Gets the class rule sets, indexed by class value. Entries may be <c>null</c> when the corresponding offset is NULL.</summary>
    /// <value>An array indexed by class value; entry <c>c</c> is the rule set for rules whose initial glyph has class <c>c</c>, or <c>null</c> when no rules start with that class.</value>
    /// <seealso cref="ClassDef"/>
    /// <seealso cref="ClassSequenceRuleSet"/>
    public IReadOnlyList<ClassSequenceRuleSet?> ClassRuleSets { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-2 body (after the format discriminant).</param>
    /// <param name="context">Forwarded to the subtable parsers.</param>
    /// <returns>The parsed format-2 subtable.</returns>
    /// <exception cref="EndOfStreamException">The header, coverage, class definition, or rule-set array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-2-class-based-glyph-contexts">Sequence Context Format 2</see> in the OpenType specification.</remarks>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="ClassDef"/>
    /// <seealso cref="ClassSequenceRuleSet"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-2-class-based-glyph-contexts">OpenType specification: Sequence Context Format 2</seealso>
    static SequenceContextFormat2 IDerivedRecord<SequenceContext, SequenceContextFormat2>.Parse(ref Cursor cursor, object? context)
    {
        Source source = cursor.Source;
        return new()
        {
            Format = 2,
            Coverage = source.ParseRecordAt<Coverage>(cursor.ReadOffset16(), context),
            ClassDef = source.ParseRecordAt<ClassDef>(cursor.ReadOffset16(), context),
            ClassRuleSets = cursor.ReadOffset16ArrayInterpret(cursor.ReadUInt16(),
                off => off != 0 ? source.ParseRecordAt<ClassSequenceRuleSet>(off, context) : null),
        };
    }
}

/// <summary>A set of <see cref="ClassSequenceRule"/> tables.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Rules within a set are evaluated in declaration order.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-2-class-based-glyph-contexts">ClassSequenceRuleSet</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SequenceContextFormat2"/>
/// <seealso cref="ClassSequenceRule"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-2-class-based-glyph-contexts">OpenType specification: ClassSequenceRuleSet</seealso>
public sealed record ClassSequenceRuleSet : IRecord<ClassSequenceRuleSet>
{
    /// <summary>Gets the rules, evaluated in declaration order.</summary>
    /// <value>The ordered list of <see cref="ClassSequenceRule"/> entries.</value>
    /// <seealso cref="ClassSequenceRule"/>
    public IReadOnlyList<ClassSequenceRule> Rules { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the rule set.</param>
    /// <param name="context">Forwarded to the rule parsers.</param>
    /// <returns>The parsed rule set.</returns>
    /// <exception cref="EndOfStreamException">The count or rule array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-2-class-based-glyph-contexts">ClassSequenceRuleSet</see> in the OpenType specification.</remarks>
    /// <seealso cref="ClassSequenceRule"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-2-class-based-glyph-contexts">OpenType specification: ClassSequenceRuleSet</seealso>
    static ClassSequenceRuleSet IRecord<ClassSequenceRuleSet>.Parse(ref Cursor cursor, object? context) =>
        new() { Rules = cursor.ReadOffset16ArrayPeekRecord<ClassSequenceRule>(cursor.ReadUInt16(), context) };
}

/// <summary>A class-based input sequence rule.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Positions in the input sequence are matched against class values rather than specific glyph IDs, so a single rule can match many glyph substitutions.</description></item>
/// <item><description>The initial position's class value selects the enclosing rule set; only positions 1..n are stored here.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-2-class-based-glyph-contexts">ClassSequenceRule</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ClassSequenceRuleSet"/>
/// <seealso cref="SequenceLookupRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-2-class-based-glyph-contexts">OpenType specification: ClassSequenceRule</seealso>
public sealed record ClassSequenceRule : IRecord<ClassSequenceRule>
{
    /// <summary>Gets the remaining class values (input positions 1..n).</summary>
    /// <value>The class values that must follow the initial position for the rule to match. Length is <c>glyphCount - 1</c>.</value>
    /// <seealso cref="LookupRecords"/>
    public IReadOnlyList<ushort> InputSequence { get; init; } = [];

    /// <summary>Gets the nested lookups to apply.</summary>
    /// <value>The ordered list of <see cref="SequenceLookupRecord"/> entries.</value>
    /// <seealso cref="InputSequence"/>
    /// <seealso cref="SequenceLookupRecord"/>
    public IReadOnlyList<SequenceLookupRecord> LookupRecords { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the rule.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed rule.</returns>
    /// <exception cref="EndOfStreamException">The header, class array, or lookup array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-2-class-based-glyph-contexts">ClassSequenceRule</see> in the OpenType specification.</remarks>
    /// <seealso cref="InputSequence"/>
    /// <seealso cref="LookupRecords"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-2-class-based-glyph-contexts">OpenType specification: ClassSequenceRule</seealso>
    static ClassSequenceRule IRecord<ClassSequenceRule>.Parse(ref Cursor cursor, object? context)
    {
        int glyphCount = cursor.ReadUInt16(), lookupCount = cursor.ReadUInt16();
        return new()
        {
            InputSequence = cursor.ReadUInt16Array(glyphCount - 1),
            LookupRecords = cursor.ReadBigEndianStructArray<SequenceLookupRecord>(lookupCount),
        };
    }
}

// ═══════════════════════════ FORMAT 3: COVERAGE-BASED GLYPH CONTEXTS ═══════════════════════════

/// <summary>Sequence context format 3: one input sequence pattern, with each position specified by a Coverage table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each input position gets its own coverage table, allowing a single rule to match many specific glyph combinations without a class definition.</description></item>
/// <item><description>Format 3 is the most compact of the three when each position accepts a small, distinct set of glyphs.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-3-coverage-based-glyph-contexts">Sequence Context Format 3</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SequenceContext"/>
/// <seealso cref="Coverage"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-3-coverage-based-glyph-contexts">OpenType specification: Sequence Context Format 3</seealso>
public sealed record SequenceContextFormat3 : SequenceContext, IDerivedRecord<SequenceContext, SequenceContextFormat3>
{
    /// <summary>Gets the coverage table for each input position, in sequence order.</summary>
    /// <value>An array of <see cref="Coverage"/> entries; position <c>i</c> of the input must match a glyph in <c>Coverages[i]</c>.</value>
    /// <seealso cref="LookupRecords"/>
    /// <seealso cref="Coverage"/>
    public IReadOnlyList<Coverage> Coverages { get; init; } = [];

    /// <summary>Gets the nested lookups to apply.</summary>
    /// <value>The ordered list of <see cref="SequenceLookupRecord"/> entries.</value>
    /// <seealso cref="Coverages"/>
    /// <seealso cref="SequenceLookupRecord"/>
    public IReadOnlyList<SequenceLookupRecord> LookupRecords { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-3 body (after the format discriminant).</param>
    /// <param name="context">Forwarded to the coverage parsers.</param>
    /// <returns>The parsed format-3 subtable.</returns>
    /// <exception cref="EndOfStreamException">The header, coverage array, or lookup array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-3-coverage-based-glyph-contexts">Sequence Context Format 3</see> in the OpenType specification.</remarks>
    /// <seealso cref="Coverages"/>
    /// <seealso cref="LookupRecords"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-3-coverage-based-glyph-contexts">OpenType specification: Sequence Context Format 3</seealso>
    static SequenceContextFormat3 IDerivedRecord<SequenceContext, SequenceContextFormat3>.Parse(
        ref Cursor cursor, object? context)
    {
        int glyphCount = cursor.ReadUInt16(), lookupCount = cursor.ReadUInt16();
        return new()
        {
            Format = 3,
            Coverages = cursor.ReadOffset16ArrayPeekRecord<Coverage>(glyphCount, context),
            LookupRecords = cursor.ReadBigEndianStructArray<SequenceLookupRecord>(lookupCount),
        };
    }
}

// ═══════════════════════════ CHAINED CONTEXTS — SHARED BASE ═══════════════════════════

/// <summary>Base for the three chained contextual subtable formats.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>A chained contextual lookup extends the plain contextual form with backtrack and lookahead sequences: glyphs before and after the input sequence must also match, but are not substituted.</description></item>
/// <item><description>Formats 1, 2, and 3 mirror their <see cref="SequenceContext"/> counterparts in structure and trade-offs.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts">Chained Sequence Context subtables</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ChainedSequenceContextFormat1"/>
/// <seealso cref="ChainedSequenceContextFormat2"/>
/// <seealso cref="ChainedSequenceContextFormat3"/>
/// <seealso cref="SequenceContext"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts">OpenType specification: Chained Sequence Context subtables</seealso>
public abstract record ChainedSequenceContext : IRecord<ChainedSequenceContext>, IBaseRecord<ChainedSequenceContext>
{
    /// <summary>Gets the subtable format number (1, 2, or 3).</summary>
    /// <value>The format discriminant from the first <c>uint16</c> of the subtable.</value>
    /// <seealso cref="ChainedSequenceContextFormat1"/>
    /// <seealso cref="ChainedSequenceContextFormat2"/>
    /// <seealso cref="ChainedSequenceContextFormat3"/>
    public ushort Format { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable.</param>
    /// <param name="context">Context forwarded to the format-specific parser; usually unused.</param>
    /// <returns>The format-specific subtable.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1, 2, or 3.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts">Chained Sequence Context subtables</see> in the OpenType specification.</remarks>
    /// <seealso cref="IBaseRecord{TBase}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts">OpenType specification: Chained Sequence Context subtables</seealso>
    static ChainedSequenceContext IRecord<ChainedSequenceContext>.Parse(ref Cursor cursor, object? context)
    {
        ushort format = cursor.ReadUInt16();
        return format switch
        {
            1 => IBaseRecord<ChainedSequenceContext>.Parse<ChainedSequenceContextFormat1>(ref cursor, context),
            2 => IBaseRecord<ChainedSequenceContext>.Parse<ChainedSequenceContextFormat2>(ref cursor, context),
            3 => IBaseRecord<ChainedSequenceContext>.Parse<ChainedSequenceContextFormat3>(ref cursor, context),
            _ => throw new InvalidDataException($"ChainedSequenceContext format {format} is not defined."),
        };
    }
}

// ═══════════════════════ CHAINED FORMAT 1: SIMPLE GLYPH CONTEXTS ═══════════════════════

/// <summary>Chained sequence context format 1: backtrack, input, and lookahead sequences defined as specific glyph IDs.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The Coverage table identifies glyphs that can begin the input sequence; a rule set is looked up by coverage index.</description></item>
/// <item><description>Each rule matches explicit glyph IDs at every position — backtrack, input, and lookahead — and lists the nested lookups to apply on success.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts">Chained Sequence Context Format 1</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ChainedSequenceContext"/>
/// <seealso cref="ChainedSequenceRuleSet"/>
/// <seealso cref="ChainedSequenceRule"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts">OpenType specification: Chained Sequence Context Format 1</seealso>
public sealed record ChainedSequenceContextFormat1 : ChainedSequenceContext, IDerivedRecord<ChainedSequenceContext, ChainedSequenceContextFormat1>
{
    /// <summary>Gets the coverage table listing initial input glyphs.</summary>
    /// <value>The <see cref="Coverage"/> that identifies glyphs eligible to start the input sequence.</value>
    /// <seealso cref="RuleSets"/>
    /// <seealso cref="Coverage"/>
    public Coverage Coverage { get; init; } = null!;

    /// <summary>Gets the rule sets, in Coverage Index order; entries may be <c>null</c>.</summary>
    /// <value>An array whose length equals the coverage table's glyph count. Entry <c>i</c> is the rule set for the glyph at coverage index <c>i</c>, or <c>null</c> when no rules apply to that glyph.</value>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="ChainedSequenceRuleSet"/>
    public IReadOnlyList<ChainedSequenceRuleSet?> RuleSets { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-1 body (after the format discriminant).</param>
    /// <param name="context">Forwarded to the subtable parsers.</param>
    /// <returns>The parsed format-1 subtable.</returns>
    /// <exception cref="EndOfStreamException">The header, coverage, or rule-set array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts">Chained Sequence Context Format 1</see> in the OpenType specification.</remarks>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="ChainedSequenceRuleSet"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts">OpenType specification: Chained Sequence Context Format 1</seealso>
    static ChainedSequenceContextFormat1 IDerivedRecord<ChainedSequenceContext, ChainedSequenceContextFormat1>.Parse(ref Cursor cursor, object? context)
    {
        Source source = cursor.Source;
        return new()
        {
            Format = 1,
            Coverage = source.ParseRecordAt<Coverage>(cursor.ReadOffset16(), context),
            RuleSets = cursor.ReadOffset16ArrayInterpret(cursor.ReadUInt16(),
                off => off != 0 ? source.ParseRecordAt<ChainedSequenceRuleSet>(off, context) : null),
        };
    }
}

/// <summary>A set of <see cref="ChainedSequenceRule"/> tables.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Rules within a set are evaluated in declaration order; the first one whose backtrack, input, and lookahead sequences match is used.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts">ChainedSequenceRuleSet</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ChainedSequenceContextFormat1"/>
/// <seealso cref="ChainedSequenceRule"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts">OpenType specification: ChainedSequenceRuleSet</seealso>
public sealed record ChainedSequenceRuleSet : IRecord<ChainedSequenceRuleSet>
{
    /// <summary>Gets the rules, evaluated in declaration order.</summary>
    /// <value>The ordered list of <see cref="ChainedSequenceRule"/> entries.</value>
    /// <seealso cref="ChainedSequenceRule"/>
    public IReadOnlyList<ChainedSequenceRule> Rules { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the rule set.</param>
    /// <param name="context">Forwarded to the rule parsers.</param>
    /// <returns>The parsed rule set.</returns>
    /// <exception cref="EndOfStreamException">The count or rule array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts">ChainedSequenceRuleSet</see> in the OpenType specification.</remarks>
    /// <seealso cref="ChainedSequenceRule"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts">OpenType specification: ChainedSequenceRuleSet</seealso>
    static ChainedSequenceRuleSet IRecord<ChainedSequenceRuleSet>.Parse(ref Cursor cursor, object? context) => new() { Rules = cursor.ReadOffset16ArrayPeekRecord<ChainedSequenceRule>(cursor.ReadUInt16(), context) };
}

/// <summary>A chained sequence rule: backtrack + input + lookahead glyph IDs plus actions.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The initial glyph of the input sequence is implicit through the enclosing coverage index; only the remaining input glyphs are stored in <see cref="InputSequence"/>.</description></item>
/// <item><description>Backtrack glyphs are stored in reverse logical order (index 0 is immediately before the input); lookahead glyphs are stored in logical order (index 0 is immediately after the input).</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts">ChainedSequenceRule</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ChainedSequenceRuleSet"/>
/// <seealso cref="SequenceLookupRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts">OpenType specification: ChainedSequenceRule</seealso>
public sealed record ChainedSequenceRule : IRecord<ChainedSequenceRule>
{
    /// <summary>Gets the backtrack glyph IDs in reverse logical order (index 0 = immediately before input).</summary>
    /// <value>The glyph IDs that must precede the input sequence, indexed from the position closest to the input outward.</value>
    /// <seealso cref="InputSequence"/>
    /// <seealso cref="LookaheadSequence"/>
    public IReadOnlyList<ushort> BacktrackSequence { get; init; } = [];

    /// <summary>Gets the remaining input glyph IDs (input positions 1..n).</summary>
    /// <value>The glyph IDs that must follow the initial glyph. Length is <c>glyphCount - 1</c>.</value>
    /// <seealso cref="BacktrackSequence"/>
    /// <seealso cref="LookaheadSequence"/>
    public IReadOnlyList<ushort> InputSequence { get; init; } = [];

    /// <summary>Gets the lookahead glyph IDs in logical order (index 0 = immediately after input).</summary>
    /// <value>The glyph IDs that must follow the input sequence, indexed from the position closest to the input outward.</value>
    /// <seealso cref="BacktrackSequence"/>
    /// <seealso cref="InputSequence"/>
    public IReadOnlyList<ushort> LookaheadSequence { get; init; } = [];

    /// <summary>Gets the nested lookups to apply.</summary>
    /// <value>The ordered list of <see cref="SequenceLookupRecord"/> entries.</value>
    /// <seealso cref="BacktrackSequence"/>
    /// <seealso cref="InputSequence"/>
    /// <seealso cref="LookaheadSequence"/>
    /// <seealso cref="SequenceLookupRecord"/>
    public IReadOnlyList<SequenceLookupRecord> LookupRecords { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the rule.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed rule.</returns>
    /// <exception cref="EndOfStreamException">The header or any of the four arrays extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts">ChainedSequenceRule</see> in the OpenType specification.</remarks>
    /// <seealso cref="BacktrackSequence"/>
    /// <seealso cref="InputSequence"/>
    /// <seealso cref="LookaheadSequence"/>
    /// <seealso cref="LookupRecords"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts">OpenType specification: ChainedSequenceRule</seealso>
    static ChainedSequenceRule IRecord<ChainedSequenceRule>.Parse(ref Cursor cursor, object? context) => new()
    {
        BacktrackSequence = cursor.ReadUInt16Array(cursor.ReadUInt16()),
        InputSequence = cursor.ReadUInt16Array(cursor.ReadUInt16() - 1),
        LookaheadSequence = cursor.ReadUInt16Array(cursor.ReadUInt16()),
        LookupRecords = cursor.ReadBigEndianStructArray<SequenceLookupRecord>(cursor.ReadUInt16()),
    };
}

// ═══════════════════════ CHAINED FORMAT 2: CLASS-BASED GLYPH CONTEXTS ═══════════════════════

/// <summary>Chained sequence context format 2: backtrack, input, and lookahead sequences defined as class values.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Three independent class definitions are used: one for the backtrack sequence, one for the input sequence, and one for the lookahead sequence.</description></item>
/// <item><description>Rule sets are indexed by the class value of the initial input glyph.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-2-class-based-glyph-contexts">Chained Sequence Context Format 2</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ChainedSequenceContext"/>
/// <seealso cref="ChainedClassSequenceRuleSet"/>
/// <seealso cref="ChainedClassSequenceRule"/>
/// <seealso cref="ClassDef"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-2-class-based-glyph-contexts">OpenType specification: Chained Sequence Context Format 2</seealso>
public sealed record ChainedSequenceContextFormat2 : ChainedSequenceContext, IDerivedRecord<ChainedSequenceContext, ChainedSequenceContextFormat2>
{
    /// <summary>Gets the coverage table listing initial input glyphs.</summary>
    /// <value>The <see cref="Coverage"/> that identifies glyphs eligible to start the input sequence.</value>
    /// <seealso cref="BacktrackClassDef"/>
    /// <seealso cref="InputClassDef"/>
    /// <seealso cref="Coverage"/>
    public Coverage Coverage { get; init; } = null!;

    /// <summary>Gets the ClassDef for the backtrack sequence.</summary>
    /// <value>The <see cref="ClassDef"/> that assigns class values to the glyphs preceding the input sequence.</value>
    /// <seealso cref="InputClassDef"/>
    /// <seealso cref="LookaheadClassDef"/>
    /// <seealso cref="ClassDef"/>
    public ClassDef BacktrackClassDef { get; init; } = null!;

    /// <summary>Gets the ClassDef for the input sequence.</summary>
    /// <value>The <see cref="ClassDef"/> that assigns class values to the glyphs of the input sequence.</value>
    /// <seealso cref="BacktrackClassDef"/>
    /// <seealso cref="LookaheadClassDef"/>
    /// <seealso cref="ClassDef"/>
    public ClassDef InputClassDef { get; init; } = null!;

    /// <summary>Gets the ClassDef for the lookahead sequence.</summary>
    /// <value>The <see cref="ClassDef"/> that assigns class values to the glyphs following the input sequence.</value>
    /// <seealso cref="BacktrackClassDef"/>
    /// <seealso cref="InputClassDef"/>
    /// <seealso cref="ClassDef"/>
    public ClassDef LookaheadClassDef { get; init; } = null!;

    /// <summary>Gets the rule sets indexed by input class value; entries may be <c>null</c>.</summary>
    /// <value>An array indexed by class value; entry <c>c</c> is the rule set for rules whose initial input glyph has class <c>c</c>, or <c>null</c> when no rules start with that class.</value>
    /// <seealso cref="InputClassDef"/>
    /// <seealso cref="ChainedClassSequenceRuleSet"/>
    public IReadOnlyList<ChainedClassSequenceRuleSet?> ClassRuleSets { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-2 body (after the format discriminant).</param>
    /// <param name="context">Forwarded to the subtable parsers.</param>
    /// <returns>The parsed format-2 subtable.</returns>
    /// <exception cref="EndOfStreamException">The header, coverage, class definitions, or rule-set array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-2-class-based-glyph-contexts">Chained Sequence Context Format 2</see> in the OpenType specification.</remarks>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="ClassDef"/>
    /// <seealso cref="ChainedClassSequenceRuleSet"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-2-class-based-glyph-contexts">OpenType specification: Chained Sequence Context Format 2</seealso>
    static ChainedSequenceContextFormat2 IDerivedRecord<ChainedSequenceContext, ChainedSequenceContextFormat2>.Parse(ref Cursor cursor, object? context)
    {
        Source source = cursor.Source;
        return new()
        {
            Format = 2,
            Coverage = source.ParseRecordAt<Coverage>(cursor.ReadOffset16(), context),
            BacktrackClassDef = source.ParseRecordAt<ClassDef>(cursor.ReadOffset16(), context),
            InputClassDef = source.ParseRecordAt<ClassDef>(cursor.ReadOffset16(), context),
            LookaheadClassDef = source.ParseRecordAt<ClassDef>(cursor.ReadOffset16(), context),
            ClassRuleSets = cursor.ReadOffset16ArrayInterpret(cursor.ReadUInt16(),
                off => off != 0 ? source.ParseRecordAt<ChainedClassSequenceRuleSet>(off, context) : null),
        };
    }
}

/// <summary>A set of <see cref="ChainedClassSequenceRule"/> tables.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Rules within a set are evaluated in declaration order.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-2-class-based-glyph-contexts">ChainedClassSequenceRuleSet</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ChainedSequenceContextFormat2"/>
/// <seealso cref="ChainedClassSequenceRule"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-2-class-based-glyph-contexts">OpenType specification: ChainedClassSequenceRuleSet</seealso>
public sealed record ChainedClassSequenceRuleSet : IRecord<ChainedClassSequenceRuleSet>
{
    /// <summary>Gets the rules, evaluated in declaration order.</summary>
    /// <value>The ordered list of <see cref="ChainedClassSequenceRule"/> entries.</value>
    /// <seealso cref="ChainedClassSequenceRule"/>
    public IReadOnlyList<ChainedClassSequenceRule> Rules { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the rule set.</param>
    /// <param name="context">Forwarded to the rule parsers.</param>
    /// <returns>The parsed rule set.</returns>
    /// <exception cref="EndOfStreamException">The count or rule array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-2-class-based-glyph-contexts">ChainedClassSequenceRuleSet</see> in the OpenType specification.</remarks>
    /// <seealso cref="ChainedClassSequenceRule"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-2-class-based-glyph-contexts">OpenType specification: ChainedClassSequenceRuleSet</seealso>
    static ChainedClassSequenceRuleSet IRecord<ChainedClassSequenceRuleSet>.Parse(ref Cursor cursor, object? context) =>
        new() { Rules = cursor.ReadOffset16ArrayPeekRecord<ChainedClassSequenceRule>(cursor.ReadUInt16(), context) };
}

/// <summary>A class-based chained sequence rule.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Positions in all three sequences are matched against class values rather than specific glyph IDs.</description></item>
/// <item><description>The initial input position's class value selects the enclosing rule set; only positions 1..n of the input are stored here.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-2-class-based-glyph-contexts">ChainedClassSequenceRule</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ChainedClassSequenceRuleSet"/>
/// <seealso cref="SequenceLookupRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-2-class-based-glyph-contexts">OpenType specification: ChainedClassSequenceRule</seealso>
public sealed record ChainedClassSequenceRule : IRecord<ChainedClassSequenceRule>
{
    /// <summary>Gets the backtrack class values in reverse logical order.</summary>
    /// <value>The class values that must precede the input sequence, indexed from the position closest to the input outward.</value>
    /// <seealso cref="InputSequence"/>
    /// <seealso cref="LookaheadSequence"/>
    public IReadOnlyList<ushort> BacktrackSequence { get; init; } = [];

    /// <summary>Gets the remaining input class values (input positions 1..n).</summary>
    /// <value>The class values that must follow the initial input position. Length is <c>glyphCount - 1</c>.</value>
    /// <seealso cref="BacktrackSequence"/>
    /// <seealso cref="LookaheadSequence"/>
    public IReadOnlyList<ushort> InputSequence { get; init; } = [];

    /// <summary>Gets the lookahead class values in logical order.</summary>
    /// <value>The class values that must follow the input sequence, indexed from the position closest to the input outward.</value>
    /// <seealso cref="BacktrackSequence"/>
    /// <seealso cref="InputSequence"/>
    public IReadOnlyList<ushort> LookaheadSequence { get; init; } = [];

    /// <summary>Gets the nested lookups to apply.</summary>
    /// <value>The ordered list of <see cref="SequenceLookupRecord"/> entries.</value>
    /// <seealso cref="BacktrackSequence"/>
    /// <seealso cref="InputSequence"/>
    /// <seealso cref="LookaheadSequence"/>
    /// <seealso cref="SequenceLookupRecord"/>
    public IReadOnlyList<SequenceLookupRecord> LookupRecords { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the rule.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed rule.</returns>
    /// <exception cref="EndOfStreamException">The header or any of the four arrays extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-2-class-based-glyph-contexts">ChainedClassSequenceRule</see> in the OpenType specification.</remarks>
    /// <seealso cref="BacktrackSequence"/>
    /// <seealso cref="InputSequence"/>
    /// <seealso cref="LookaheadSequence"/>
    /// <seealso cref="LookupRecords"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-2-class-based-glyph-contexts">OpenType specification: ChainedClassSequenceRule</seealso>
    static ChainedClassSequenceRule IRecord<ChainedClassSequenceRule>.Parse(ref Cursor cursor, object? context) => new()
    {
        BacktrackSequence = cursor.ReadUInt16Array(cursor.ReadUInt16()),
        InputSequence = cursor.ReadUInt16Array(cursor.ReadUInt16() - 1),
        LookaheadSequence = cursor.ReadUInt16Array(cursor.ReadUInt16()),
        LookupRecords = cursor.ReadBigEndianStructArray<SequenceLookupRecord>(cursor.ReadUInt16()),
    };
}

// ═══════════════════════ CHAINED FORMAT 3: COVERAGE-BASED GLYPH CONTEXTS ═══════════════════════

/// <summary>Chained sequence context format 3: one input pattern plus its backtrack and lookahead sequences, each position specified by a Coverage table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Every position in all three sequences gets its own coverage table, giving the format maximum precision without requiring class definitions.</description></item>
/// <item><description>Like the non-chained format 3, this is the most compact form when each position accepts a small, distinct set of glyphs.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-3-coverage-based-glyph-contexts">Chained Sequence Context Format 3</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ChainedSequenceContext"/>
/// <seealso cref="Coverage"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-3-coverage-based-glyph-contexts">OpenType specification: Chained Sequence Context Format 3</seealso>
public sealed record ChainedSequenceContextFormat3 : ChainedSequenceContext, IDerivedRecord<ChainedSequenceContext, ChainedSequenceContextFormat3>
{
    /// <summary>Gets the coverage tables for the backtrack sequence, in the order the specification declares them (backtrack index 0 is immediately before the input).</summary>
    /// <value>The ordered array of <see cref="Coverage"/> entries for the backtrack sequence, indexed from the position closest to the input outward.</value>
    /// <seealso cref="InputCoverages"/>
    /// <seealso cref="LookaheadCoverages"/>
    /// <seealso cref="Coverage"/>
    public IReadOnlyList<Coverage> BacktrackCoverages { get; init; } = [];

    /// <summary>Gets the coverage tables for the input sequence, in logical order.</summary>
    /// <value>The ordered array of <see cref="Coverage"/> entries; position <c>i</c> of the input must match a glyph in <c>InputCoverages[i]</c>.</value>
    /// <seealso cref="BacktrackCoverages"/>
    /// <seealso cref="LookaheadCoverages"/>
    /// <seealso cref="Coverage"/>
    public IReadOnlyList<Coverage> InputCoverages { get; init; } = [];

    /// <summary>Gets the coverage tables for the lookahead sequence, in logical order.</summary>
    /// <value>The ordered array of <see cref="Coverage"/> entries for the lookahead sequence, indexed from the position closest to the input outward.</value>
    /// <seealso cref="BacktrackCoverages"/>
    /// <seealso cref="InputCoverages"/>
    /// <seealso cref="Coverage"/>
    public IReadOnlyList<Coverage> LookaheadCoverages { get; init; } = [];

    /// <summary>Gets the nested lookups to apply.</summary>
    /// <value>The ordered list of <see cref="SequenceLookupRecord"/> entries.</value>
    /// <seealso cref="InputCoverages"/>
    /// <seealso cref="SequenceLookupRecord"/>
    public IReadOnlyList<SequenceLookupRecord> LookupRecords { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-3 body (after the format discriminant).</param>
    /// <param name="context">Forwarded to the coverage parsers.</param>
    /// <returns>The parsed format-3 subtable.</returns>
    /// <exception cref="EndOfStreamException">The header, any coverage array, or the lookup array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-3-coverage-based-glyph-contexts">Chained Sequence Context Format 3</see> in the OpenType specification.</remarks>
    /// <seealso cref="BacktrackCoverages"/>
    /// <seealso cref="InputCoverages"/>
    /// <seealso cref="LookaheadCoverages"/>
    /// <seealso cref="LookupRecords"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-3-coverage-based-glyph-contexts">OpenType specification: Chained Sequence Context Format 3</seealso>
    static ChainedSequenceContextFormat3 IDerivedRecord<ChainedSequenceContext, ChainedSequenceContextFormat3>.Parse(ref Cursor cursor, object? context) => new()
    {
        Format = 3,
        BacktrackCoverages = cursor.ReadOffset16ArrayPeekRecord<Coverage>(cursor.ReadUInt16(), context),
        InputCoverages = cursor.ReadOffset16ArrayPeekRecord<Coverage>(cursor.ReadUInt16(), context),
        LookaheadCoverages = cursor.ReadOffset16ArrayPeekRecord<Coverage>(cursor.ReadUInt16(), context),
        LookupRecords = cursor.ReadBigEndianStructArray<SequenceLookupRecord>(cursor.ReadUInt16()),
    };
}
