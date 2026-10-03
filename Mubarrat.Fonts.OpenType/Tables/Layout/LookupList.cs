using Mubarrat.Fonts.OpenType.Binary;

namespace Mubarrat.Fonts.OpenType.Tables.Layout;

/// <summary>An array of offsets to Lookup tables, in processing order. The subtable type is fixed at compile time by the caller, which knows the mapping from a lookup's <c>LookupType</c> to its subtable representation.</summary>
/// <typeparam name="TSubtable">The common base of every subtable a lookup in this list may contain. For GSUB, this is typically an abstract <c>GsubSubtable</c> with one subtype per lookup type; for GPOS, the equivalent <c>GposSubtable</c>.</typeparam>
/// <remarks>
/// <list type="bullet">
/// <item><description>The list is referenced by <see cref="Feature.LookupIndices"/> entries; an index into the list selects the lookup that implements that feature.</description></item>
/// <item><description>Lookups are applied in the order they appear in the list, and the specification permits one lookup to invoke another via a nested <c>SequenceLookupRecord</c>; the containing table detects cycles at shaping time.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-list-table">Lookup list table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Lookup{TSubtable}"/>
/// <seealso cref="Feature.LookupIndices"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-list-table">OpenType specification: Lookup list table</seealso>
public sealed record LookupList<TSubtable> : IRecord<LookupList<TSubtable>>
    where TSubtable : IRecord<TSubtable>
{
    /// <summary>Gets the lookups, fully parsed in list order.</summary>
    /// <value>The ordered list of <see cref="Lookup{TSubtable}"/> entries; index <c>i</c> corresponds to the lookup index that <see cref="Feature.LookupIndices"/> references.</value>
    /// <seealso cref="GetLookup(int)"/>
    /// <seealso cref="Count"/>
    public IReadOnlyList<Lookup<TSubtable>> Lookups { get; init; } = [];

    /// <summary>Gets the number of lookups.</summary>
    /// <value>The size of the <see cref="Lookups"/> list.</value>
    /// <seealso cref="Lookups"/>
    public int Count => Lookups.Count;

    /// <summary>Returns the Lookup at an index, or <c>null</c> when the index is out of range.</summary>
    /// <param name="index">The zero-based lookup index.</param>
    /// <returns>The <see cref="Lookup{TSubtable}"/> at <paramref name="index"/>, or <c>null</c> when the index is out of range.</returns>
    /// <remarks>The lookup is constant-time; the index directly selects an entry from the ordered list.</remarks>
    /// <seealso cref="Lookups"/>
    /// <seealso cref="Lookup{TSubtable}"/>
    public Lookup<TSubtable>? GetLookup(int index) =>
        (uint)index < (uint)Lookups.Count ? Lookups[index] : null;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the lookup list.</param>
    /// <param name="context">Forwarded to the lookup parsers, typically a <see cref="SubtableContext"/> or a parent context.</param>
    /// <returns>The parsed lookup list with its lookups fully resolved.</returns>
    /// <exception cref="EndOfStreamException">The count, offset array, or any referenced lookup extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-list-table">Lookup list table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Lookups"/>
    /// <seealso cref="Lookup{TSubtable}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-list-table">OpenType specification: Lookup list table</seealso>
    public static LookupList<TSubtable> Parse(ref Cursor cursor, object? context) => new() { Lookups = cursor.ReadOffset16ArrayPeekRecord<Lookup<TSubtable>>(cursor.ReadUInt16(), context) };
}

/// <summary>A Lookup table: a type and the subtables that implement it. The subtables are eagerly parsed at construction; the caller supplies the subtable base type via <typeparamref name="TSubtable"/>.</summary>
/// <typeparam name="TSubtable">The common base of the lookup's subtables. All subtables of one lookup share the same base type; the specific subtype is determined by the subtable's own format field, which the subtable base's <c>Parse</c> dispatches on.</typeparam>
/// <remarks>
/// <list type="bullet">
/// <item><description>The lookup's <see cref="LookupType"/> tells the reader which subtable family to expect. The dispatcher that constructs a <see cref="Lookup{TSubtable}"/> uses that to choose <typeparamref name="TSubtable"/>. For a GSUB lookup of type 1, the subtable type is the GSUB single-substitution base; for a type 4 lookup, it is the ligature-substitution base; and so on.</description></item>
/// <item><description>Because the subtable base is a type parameter and the subtable's own format field drives the concrete dispatch, one lookup cannot contain subtables from two different lookup types. That matches the specification: all subtables of a lookup share its <c>LookupType</c>.</description></item>
/// <item><description>The <see cref="MarkFilteringSet"/> field is present only when <see cref="LookupFlag.UseMarkFilteringSet"/> is set; the parser reads it conditionally after the subtable array.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table">Lookup table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LookupList{TSubtable}"/>
/// <seealso cref="LookupFlag"/>
/// <seealso cref="SubtableContext"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table">OpenType specification: Lookup table</seealso>
public sealed record Lookup<TSubtable> : IRecord<Lookup<TSubtable>>
    where TSubtable : IRecord<TSubtable>
{
    /// <summary>Gets the lookup type. Its meaning depends on the containing table.</summary>
    /// <value>The lookup type discriminant used to select the subtable family; ranges from 1 upward according to the enclosing table (GSUB or GPOS).</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table"><c>lookupType</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="SubtableContext.LookupType"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table">OpenType specification: <c>lookupType</c></seealso>
    public ushort LookupType { get; init; }

    /// <summary>Gets the lookup flags.</summary>
    /// <value>The <see cref="LookupFlag"/> bits that control which glyphs the lookup applies to and how it interacts with mark filtering.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table"><c>lookupFlag</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="LookupFlag"/>
    /// <seealso cref="MarkFilteringSet"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table">OpenType specification: <c>lookupFlag</c></seealso>
    public LookupFlag Flags { get; init; }

    /// <summary>Gets the mark filtering set index, or <c>null</c> when not present.</summary>
    /// <value>The zero-based index into the enclosing GDEF table's mark glyph sets, or <c>null</c> when <see cref="LookupFlag.UseMarkFilteringSet"/> is not set on <see cref="Flags"/>.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table"><c>markFilteringSet</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Flags"/>
    /// <seealso cref="LookupFlag.UseMarkFilteringSet"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table">OpenType specification: <c>markFilteringSet</c></seealso>
    public ushort? MarkFilteringSet { get; init; }

    /// <summary>Gets the subtables, fully parsed in lookup order.</summary>
    /// <value>The ordered list of <typeparamref name="TSubtable"/> entries. All subtables share the same type family, selected by <see cref="LookupType"/>.</value>
    /// <seealso cref="GetSubtable(int)"/>
    /// <seealso cref="SubtableCount"/>
    public IReadOnlyList<TSubtable> Subtables { get; init; } = [];

    /// <summary>Gets the number of subtables.</summary>
    /// <value>The size of the <see cref="Subtables"/> list.</value>
    /// <seealso cref="Subtables"/>
    public int SubtableCount => Subtables.Count;

    /// <summary>Returns the subtable at an index, or <c>null</c> when the index is out of range.</summary>
    /// <param name="index">The zero-based subtable index within the lookup.</param>
    /// <returns>The <typeparamref name="TSubtable"/> at <paramref name="index"/>, or <c>null</c> when the index is out of range.</returns>
    /// <remarks>The lookup is constant-time; the index directly selects an entry from the ordered list.</remarks>
    /// <seealso cref="Subtables"/>
    public TSubtable? GetSubtable(int index) =>
        (uint)index < (uint)Subtables.Count ? Subtables[index] : default;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the lookup record.</param>
    /// <param name="context">Forwarded to the subtable parsers.</param>
    /// <returns>The parsed lookup with its subtables fully resolved.</returns>
    /// <exception cref="EndOfStreamException">The header, subtable array, or any referenced subtable extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The subtable family is selected by the lookup's own <see cref="LookupType"/> field, which is wrapped into a <see cref="SubtableContext"/> and passed to each subtable's parse.</description></item>
    /// <item><description>The <see cref="MarkFilteringSet"/> field is read only when <see cref="LookupFlag.UseMarkFilteringSet"/> is set; on all other lookups the cursor is left immediately after the subtable offset array.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table">Lookup table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SubtableContext"/>
    /// <seealso cref="LookupFlag"/>
    /// <seealso cref="Subtables"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table">OpenType specification: Lookup table</seealso>
    public static Lookup<TSubtable> Parse(ref Cursor cursor, object? context)
    {
        ushort lookupType = cursor.ReadUInt16();
        var flags = (LookupFlag)cursor.ReadUInt16();
        ushort subtableCount = cursor.ReadUInt16();
        var subtables = cursor.ReadOffset16ArrayPeekRecord<TSubtable>(subtableCount, new SubtableContext(lookupType));

        ushort? markFilteringSet = null;
        if ((flags & LookupFlag.UseMarkFilteringSet) != 0)
            markFilteringSet = cursor.ReadUInt16();

        return new Lookup<TSubtable>
        {
            LookupType = lookupType,
            Flags = flags,
            MarkFilteringSet = markFilteringSet,
            Subtables = subtables,
        };
    }
}

/// <summary>Context carrying the enclosing lookup's type, which the subtable dispatchers use to select the concrete subtable family.</summary>
/// <param name="LookupType">The lookup type from the enclosing <see cref="Lookup{TSubtable}"/> record.</param>
/// <remarks>
/// <list type="bullet">
/// <item><description>Shared between the GSUB and GPOS dispatchers; the meaning of <see cref="LookupType"/> depends on which table's lookup list is being parsed.</description></item>
/// <item><description>Also used when re-dispatching through an extension subtable; the extension's <c>extensionLookupType</c> becomes the new <see cref="LookupType"/>.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Lookup{TSubtable}.LookupType"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table">OpenType specification: Lookup table</seealso>
public record SubtableContext(ushort LookupType);

/// <summary>Bit flags for a Lookup table's <c>lookupFlag</c> field.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The first five bits control glyph filtering and lookup direction; bits 5–7 are reserved; bits 8–15 carry a mark attachment type mask.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table"><c>lookupFlag</c> field</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Lookup{TSubtable}.Flags"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table">OpenType specification: <c>lookupFlag</c></seealso>
[Flags]
public enum LookupFlag : ushort
{
    /// <summary>No flags set.</summary>
    /// <remarks>The lookup applies to every glyph and processes left-to-right. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table"><c>lookupFlag</c> field</see>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table">OpenType specification: <c>lookupFlag</c></seealso>
    None = 0,

    /// <summary>The lookup is applied right-to-left.</summary>
    /// <remarks>Bit 0. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table"><c>RIGHT_TO_LEFT</c> flag</see>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table">OpenType specification: <c>RIGHT_TO_LEFT</c></seealso>
    RightToLeft = 0x0001,

    /// <summary>Skip base glyphs.</summary>
    /// <remarks>Bit 1. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table"><c>IGNORE_BASE_GLYPHS</c> flag</see>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table">OpenType specification: <c>IGNORE_BASE_GLYPHS</c></seealso>
    IgnoreBaseGlyphs = 0x0002,

    /// <summary>Skip ligature glyphs.</summary>
    /// <remarks>Bit 2. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table"><c>IGNORE_LIGATURES</c> flag</see>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table">OpenType specification: <c>IGNORE_LIGATURES</c></seealso>
    IgnoreLigatures = 0x0004,

    /// <summary>Skip combining marks.</summary>
    /// <remarks>Bit 3. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table"><c>IGNORE_MARKS</c> flag</see>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table">OpenType specification: <c>IGNORE_MARKS</c></seealso>
    IgnoreMarks = 0x0008,

    /// <summary>The lookup includes a MarkFilteringSet field.</summary>
    /// <remarks>Bit 4. When set, the parser reads an additional <c>uint16</c> field after the subtable array. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table"><c>USE_MARK_FILTERING_SET</c> flag</see>.</remarks>
    /// <seealso cref="Lookup{TSubtable}.MarkFilteringSet"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table">OpenType specification: <c>USE_MARK_FILTERING_SET</c></seealso>
    UseMarkFilteringSet = 0x0010,

    /// <summary>Bits 5–7: reserved.</summary>
    /// <remarks>Set to zero by conforming writers.</remarks>
    Reserved = 0x00E0,

    /// <summary>Bits 8–15: mark attachment type. Zero means all marks are eligible.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>This is a mask, not a flag: the field's value selects a mark attachment class from the enclosing GDEF table.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table"><c>markAttachmentType</c> bits</see>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Lookup{TSubtable}.Flags"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table">OpenType specification: <c>markAttachmentType</c></seealso>
    MarkAttachmentTypeMask = 0xFF00,
}
