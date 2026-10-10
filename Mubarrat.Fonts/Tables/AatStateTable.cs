using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// AAT shared state-table machinery — class subtables, extended state headers, transitions
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The class subtable of an Apple Advanced Typography state table: a lookup table that maps a glyph index onto the glyph class the state machine treats it as.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>A state table divides the glyphs of a font into classes so that one transition rule can cover many glyphs. The manual describes the class table of an extended state table as being "simply <c>LookupTable</c>s", where the looked-up value is a 16-bit class value; in the original (non-extended) format the same information is a trimmed array of 8-bit class codes, which a format 8 lookup table reproduces exactly.</description></item>
/// <item><description>Three class codes are reserved and are therefore never supplied by the table itself: <see cref="AatClass.EndOfText"/> for the end of the glyph array, <see cref="AatClass.OutOfBounds"/> for a glyph the table does not cover, and <see cref="AatClass.DeletedGlyph"/> for a glyph an earlier action removed. A table's own classes begin at <see cref="AatClass.FirstFree"/>. <see cref="GetGlyphClass(int)"/> therefore answers <see cref="AatClass.OutOfBounds"/> rather than <see langword="false"/> for an uncovered glyph.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, The Class Subtable and Extended State Tables</see> for the definition.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LookupTable"/>
/// <seealso cref="AatStateTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Extended State Tables</seealso>
public sealed record ClassTable
{
    /// <summary>Gets the lookup table whose looked-up value is the glyph class.</summary>
    /// <remarks>Any of the formats the manual defines can appear here; formats 0 and 8 are the ones that cover a dense glyph range and so reproduce the trimmed class array of the original format.</remarks>
    /// <seealso cref="GetGlyphClass(int)"/>
    /// <seealso cref="LookupTable"/>
    public required LookupTable Lookup { get; init; }

    /// <summary>Gets the glyph class of a glyph index, or <see cref="AatClass.OutOfBounds"/> when the class table does not cover it.</summary>
    /// <param name="glyphIndex">The glyph index to classify.</param>
    /// <returns>The glyph's class code, or <see cref="AatClass.OutOfBounds"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup value is narrowed to 16 bits, because every class code a state table can address is 16 bits wide; a class table that yielded a wider value would be malformed.</description></item>
    /// <item><description>The reserved classes <see cref="AatClass.EndOfText"/> and <see cref="AatClass.DeletedGlyph"/> are supplied by the driver of the state machine, not by this method.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Lookup"/>
    /// <seealso cref="AatClass"/>
    public ushort GetGlyphClass(int glyphIndex) =>
        Lookup.TryGetValue(glyphIndex, out uint value) ? (ushort)value : AatClass.OutOfBounds;

    /// <summary>Reads a class subtable at an absolute offset.</summary>
    /// <param name="cursor">The cursor over the table the class subtable lives in. Only its <see cref="Cursor.Source"/> is used; its position is not changed.</param>
    /// <param name="offset">The absolute offset of the class subtable's lookup table within <paramref name="cursor"/>'s source.</param>
    /// <param name="glyphCount">The font's glyph count, used to bound a format 0 lookup, or <c>0</c> when no glyph count is known.</param>
    /// <returns>The parsed class subtable.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="InvalidDataException">The lookup format is not one of 0, 2, 4, 6, 8, or 10, or a format 10 unit size is not 1, 2, 4, or 8.</exception>
    /// <exception cref="EndOfStreamException">The lookup extends past the end of the table-scoped source.</exception>
    /// <remarks>A format 0 lookup declares no length of its own, so <paramref name="glyphCount"/> — which the owning table reads from <c>maxp</c> — is what tells it how many class values to read; <c>0</c> means the array runs to the end of the source.</remarks>
    /// <seealso cref="LookupTable.Parse(ref Cursor, object?)"/>
    /// <seealso cref="AatStateTable.Read(ref Cursor, long, int, int)"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Lookup Tables</seealso>
    public static ClassTable Parse(ref Cursor cursor, long offset, int glyphCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);

        Cursor lookupCursor = cursor.Source.CreateCursor(offset);
        object? context = glyphCount > 0 ? (object)glyphCount : null;

        return new ClassTable { Lookup = LookupTable.Parse(ref lookupCursor, context) };
    }
}

/// <summary>The <c>STXHeader</c> structure: the extended state table header that uses 32-bit offsets and 16-bit state array entries.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The extended state table replaced the original header's 16-bit <c>stateSize</c> word and offsets with four 32-bit words, so that a state array can hold 16-bit entry indices and a class table can be a lookup table rather than a dense array. It is the header used by the <c>morx</c> table's state-based subtables and by the <c>kerx</c> table's format 1 and format 4 subtables. The short form is <see cref="StateHeader"/>.</description></item>
/// <item><description>All three offsets are relative to the start of this header — that is, to the state table's own first byte — and not to the start of the enclosing table or subtable. The three pieces they point at (class table, state array, entry table) may appear in any order after the header.</description></item>
/// <item><description>The header is 16 bytes, so it is already four-byte aligned and carries no padding of its own. The manual's alignment rule applies to what follows a state table: its class, state array, and entry tables must be word aligned, and the per-glyph lookup tables that follow them are padded to a <c>UInt32</c> boundary.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Extended State Tables</see> for the definition of this structure.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="StateHeader"/>
/// <seealso cref="AatStateTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Extended State Tables</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct StateHeaderExtended : IEndianReversibleStruct<StateHeaderExtended>
{
    /// <summary>The size of the structure, in bytes.</summary>
    /// <remarks>Four 32-bit words, with no padding.</remarks>
    public const int SizeInBytes = 16;

    /// <summary>The number of classes, which is also the number of 16-bit entry indices in one row of the state array, at byte offset 0.</summary>
    /// <remarks>Every state table reserves at least the classes <see cref="AatClass.EndOfText"/>, <see cref="AatClass.OutOfBounds"/>, and <see cref="AatClass.DeletedGlyph"/>, so a table with <c>n</c> classes of its own declares <c>n + 3</c>.</remarks>
    /// <seealso cref="AatStateTable.ClassCount"/>
    public uint ClassCount;         // +0

    /// <summary>The offset of the class table from the start of this header at byte offset 4.</summary>
    /// <remarks>The class table is a <see cref="LookupTable"/> whose looked-up value is a 16-bit class.</remarks>
    /// <seealso cref="ClassTable"/>
    public uint ClassTableOffset;   // +4

    /// <summary>The offset of the state array from the start of this header at byte offset 8.</summary>
    /// <remarks>The state array is <c>classCount</c> 16-bit entry indices per state, and the number of states is not declared anywhere.</remarks>
    /// <seealso cref="AatStateTable.StateArray"/>
    public uint StateArrayOffset;   // +8

    /// <summary>The offset of the entry table from the start of this header at byte offset 12.</summary>
    /// <remarks>The entry table holds one entry per state and class pair. Its entry layout belongs to the table that owns the state table, so it is read as a caller-specified type through <see cref="AatStateTable.ReadEntry{T}(int)"/>.</remarks>
    /// <seealso cref="AatStateTable.EntryTableOffset"/>
    public uint EntryTableOffset;   // +12

    /// <summary>Reverses the byte order of every field in a <see cref="StateHeaderExtended"/>.</summary>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new header with every field reversed.</returns>
    /// <remarks>All four fields are <c>uint32</c> and are reversed independently. The reversal is unconditional; the host-endianness check is applied by the reader.</remarks>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Source.ReadEndianReversibleStructAt{T}(long)"/>
    public static StateHeaderExtended ReverseEndianness(StateHeaderExtended v) => new()
    {
        ClassCount = BinaryPrimitives.ReverseEndianness(v.ClassCount),
        ClassTableOffset = BinaryPrimitives.ReverseEndianness(v.ClassTableOffset),
        StateArrayOffset = BinaryPrimitives.ReverseEndianness(v.StateArrayOffset),
        EntryTableOffset = BinaryPrimitives.ReverseEndianness(v.EntryTableOffset),
    };
}

/// <summary>One entry of an Apple Advanced Typography state table entry subtable: the state to move to and the table-specific action flags.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The manual defines every entry as <c>uint16 newState</c>, <c>uint16 flags</c>, then an optional run of table-specific words holding per-glyph offsets or indices. This type is that two-word prefix, which is all a rearrangement state machine needs; a table whose entries carry further words reads a wider type through <see cref="AatStateTable.ReadEntry{T}(int)"/>.</description></item>
/// <item><description>The meaning of <see cref="Flags"/> is entirely table-specific. The <c>mort</c>, <c>kerx</c>, <c>just</c>, and <c>morx</c> pages each define their own bits.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, The Entry Subtable</see> for the definition of this structure.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AatStateTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: The Entry Subtable</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct GlyphStateEntry : IEndianReversibleStruct<GlyphStateEntry>
{
    /// <summary>The size of the structure, in bytes.</summary>
    /// <remarks>Two 16-bit words. An entry subtable for a specific table is at least this wide, which is the floor <see cref="AatStateTable.Read(ref Cursor, long, int, int)"/> enforces on an entry size.</remarks>
    public const int SizeInBytes = 4;

    /// <summary>The state to use for the next glyph at byte offset 0.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>In an extended state table — the header <see cref="StateHeaderExtended"/> describes, used by <c>morx</c> and by <c>kerx</c> formats 1 and 4 — this is a zero-based row index into the state array.</description></item>
    /// <item><description>In the original state table format, reached through <see cref="AatStateTable.ReadFromShortHeader(ref Cursor, long, StateHeader, int, int)"/>, this is a byte offset from the beginning of the state table to the new state instead. The manual notes that the offset form exists so the new row's address can be formed by adding the offset to the class code, which is why the two forms must not be mixed.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="AatStateTable.StateArray"/>
    public ushort NewState; // +0

    /// <summary>The table-specific action flags at byte offset 2.</summary>
    /// <remarks>The flags usually carry a "do not advance" bit, a "mark this glyph" bit, and a verb or index field. Their layout is given on the page of the table that defines the state machine.</remarks>
    public ushort Flags;    // +2

    /// <summary>Reverses the byte order of both fields in a <see cref="GlyphStateEntry"/>.</summary>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new entry with both fields reversed.</returns>
    /// <remarks>Both fields are <c>uint16</c> and are reversed independently. The reversal is unconditional; the host-endianness check is applied by the reader.</remarks>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Source.ReadEndianReversibleStructAt{T}(long)"/>
    public static GlyphStateEntry ReverseEndianness(GlyphStateEntry v) => new()
    {
        NewState = BinaryPrimitives.ReverseEndianness(v.NewState),
        Flags = BinaryPrimitives.ReverseEndianness(v.Flags),
    };
}

/// <summary>The shared state machinery of an Apple Advanced Typography state table: the class subtable, the state array of entry indices, and access to an entry table whose entry layout belongs to the owning table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>A state table is a finite state machine over the glyph array. It maps each glyph to a class, looks up the entry table row named by the current state and that class, applies whatever action the entry's flags describe, and moves to the state the entry names. The class array and the state array are format-defined; the actions are not, which is why this type exposes the entry table as raw entries of a caller-specified type rather than as one fixed record.</description></item>
/// <item><description>The state array is flattened row-major: the entry index for a state and glyph class is at <c>state * ClassCount + glyphClass</c>. Use <see cref="GetEntryIndex(ushort, ushort)"/> rather than indexing <see cref="StateArray"/> by hand.</description></item>
/// <item><description>The predefined classes are listed on <see cref="AatClass"/>. State 0 is the start-of-text state and state 1 is the start-of-line state; the manual calls them the two predefined states and leaves every later row to the font.</description></item>
/// <item><description>A state table is embedded in the table that owns it, so the offsets in its header are relative to the header itself. <see cref="Read(ref Cursor, long, int, int)"/> takes the header's absolute offset for that reason.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, State Tables and Extended State Tables</see> for the model, and the <c>morx</c>, <c>kerx</c>, and <c>just</c> pages for the entry layouts.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // One transition of a state machine whose entries are the shared two-word prefix.
/// ushort glyphClass = stateTable.GetGlyphClass(glyphIndex);
/// if (stateTable.TryReadEntry(state, glyphClass, out GlyphStateEntry entry))
/// {
///     ApplyFlags(entry.Flags);
///     state = entry.NewState;
/// }
/// </code>
/// </example>
/// <seealso cref="StateHeaderExtended"/>
/// <seealso cref="StateHeader"/>
/// <seealso cref="GlyphStateEntry"/>
/// <seealso cref="AatClass"/>
/// <seealso cref="LookupTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: State Tables</seealso>
public sealed record AatStateTable
{
    /// <summary>The entry index that stands for "no entry", <c>0xFFFF</c>.</summary>
    /// <remarks><see cref="GetEntryIndex(ushort, ushort)"/> answers this value when the state or the glyph class is out of range, and <see cref="TryReadEntry{T}(ushort, ushort, out T)"/> fails on it. A state array that literally stored <c>0xFFFF</c> would be indistinguishable from "no entry", which the format accepts because an entry table with 65,536 rows is not a practical font.</remarks>
    public const ushort NoEntryIndex = 0xFFFF;

    /// <summary>Gets the number of classes, which is the width of one state array row.</summary>
    /// <remarks>This is the extended header's <c>nClasses</c> field, or the short header's class count. Every state table reserves at least three classes for <see cref="AatClass"/>'s predefined codes.</remarks>
    /// <seealso cref="StateArray"/>
    /// <seealso cref="GetGlyphClass(int)"/>
    public uint ClassCount { get; init; }

    /// <summary>Gets the class lookup table that maps glyph indices onto classes.</summary>
    /// <remarks>A glyph the lookup does not cover belongs to the out-of-bounds class, <see cref="AatClass.OutOfBounds"/>; <see cref="GetGlyphClass(int)"/> applies that rule. Wrapping the lookup in <see cref="Mubarrat.Fonts.Tables.ClassTable"/> gives the same answer with the class subtable's own type.</remarks>
    /// <seealso cref="GetGlyphClass(int)"/>
    /// <seealso cref="LookupTable"/>
    public required LookupTable ClassTable { get; init; }

    /// <summary>Gets the state array, flattened row-major, as zero-based entry table row indices.</summary>
    /// <remarks>The row count is <see cref="StateCount"/>; index <c>state * ClassCount + glyphClass</c> is the entry row for that state and class. The array is kept flat because a state table's row count is not declared in the font and therefore has to be derived while parsing.</remarks>
    /// <seealso cref="StateCount"/>
    /// <seealso cref="GetEntryIndex(ushort, ushort)"/>
    public IReadOnlyList<ushort> StateArray { get; init; } = [];

    /// <summary>Gets the number of states, which is the number of state array rows.</summary>
    /// <remarks>Derived as <c>StateArray.Count / ClassCount</c>, and <c>0</c> when the class count is zero. State 0 is the start-of-text state and state 1 the start-of-line state; the rest are the font's own.</remarks>
    /// <seealso cref="StateArray"/>
    public int StateCount => ClassCount == 0 ? 0 : checked((int)(StateArray.Count / ClassCount));

    /// <summary>Gets the size, in bytes, of one entry table row.</summary>
    /// <remarks>The entry layout is table-specific, so the size is supplied by the owning table's parser rather than read from the font.</remarks>
    /// <seealso cref="ReadEntry{T}(int)"/>
    public int EntrySize { get; init; }

    /// <summary>Gets the byte source the entry table lives in, or <see langword="null"/> when the state table was built by hand.</summary>
    /// <remarks>Retained so that <see cref="ReadEntry{T}(int)"/> can decode an entry lazily, in the caller's element type.</remarks>
    /// <seealso cref="ReadEntry{T}(int)"/>
    /// <seealso cref="EntryTableOffset"/>
    public Source? Source { get; init; }

    /// <summary>Gets the absolute offset of the entry table within <see cref="Source"/>.</summary>
    /// <remarks>The header's <c>entryTableOffset</c> is relative to the state table header, so this value is the header's offset plus that field.</remarks>
    /// <seealso cref="ReadEntry{T}(int)"/>
    /// <seealso cref="Source"/>
    public long EntryTableOffset { get; init; }

    /// <summary>Gets the glyph class of a glyph index, or <see cref="AatClass.OutOfBounds"/> when the class table does not cover it.</summary>
    /// <param name="glyphIndex">The glyph index to classify.</param>
    /// <returns>The glyph's class code, or <see cref="AatClass.OutOfBounds"/>.</returns>
    /// <remarks>The reserved <see cref="AatClass.EndOfText"/> and <see cref="AatClass.DeletedGlyph"/> classes are supplied by the driver of the state machine when the glyph array is exhausted or a glyph was deleted, not by the class table.</remarks>
    /// <seealso cref="GetEntryIndex(ushort, ushort)"/>
    /// <seealso cref="AatClass"/>
    public ushort GetGlyphClass(int glyphIndex) =>
        ClassTable.TryGetValue(glyphIndex, out uint value) ? (ushort)value : AatClass.OutOfBounds;

    /// <summary>Gets the entry table row index for a state and glyph class.</summary>
    /// <param name="state">The state, which is a row of the state array.</param>
    /// <param name="glyphClass">The glyph class, which is a column of the state array.</param>
    /// <returns>The entry table row index, or <see cref="NoEntryIndex"/> when the class count is zero, the class is at least the class count, or the state and class together do not name a state array element.</returns>
    /// <remarks>The state array row count is not declared in the font, so a state beyond the rows that could be parsed simply fails rather than throwing. A transition is the pair of this call and <see cref="ReadEntry{T}(int)"/>: find the entry, apply its flags, then continue from the state its <see cref="GlyphStateEntry.NewState"/> field names.</remarks>
    /// <seealso cref="StateArray"/>
    /// <seealso cref="ReadEntry{T}(int)"/>
    /// <seealso cref="TryReadEntry{T}(ushort, ushort, out T)"/>
    public ushort GetEntryIndex(ushort state, ushort glyphClass)
    {
        if (ClassCount == 0 || glyphClass >= ClassCount) return NoEntryIndex;

        long index = (long)state * ClassCount + glyphClass;
        return index >= 0 && index < StateArray.Count ? StateArray[(int)index] : NoEntryIndex;
    }

    /// <summary>Gets one entry of the entry table.</summary>
    /// <typeparam name="T">The entry type the owning table defines. Must be a big-endian, unpadded wire struct that satisfies <see cref="IEndianReversibleStruct{T}"/>.</typeparam>
    /// <param name="index">The zero-based entry table row index, as returned by <see cref="GetEntryIndex(ushort, ushort)"/>.</param>
    /// <returns>The entry, decoded as <typeparamref name="T"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The state table has no <see cref="Source"/> to read from.</exception>
    /// <exception cref="EndOfStreamException">The entry extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The entry table is stored as raw bytes rather than a parsed array because its element type is a function of the table that owns the state table, which the state table itself does not know.</description></item>
    /// <item><description>The read is bounds-checked against <see cref="Source"/> but not against the owning subtable, so a caller that needs the subtable's limits enforced should validate <paramref name="index"/> first.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetEntryIndex(ushort, ushort)"/>
    /// <seealso cref="GlyphStateEntry"/>
    /// <seealso cref="EntrySize"/>
    public T ReadEntry<T>(int index) where T : unmanaged, IEndianReversibleStruct<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        Source? source = Source;
        if (source is null)
            throw new InvalidOperationException("The AAT state table's entry table cannot be read because the state table has no source.");

        return source.ReadEndianReversibleStructAt<T>(EntryTableOffset + (long)index * EntrySize);
    }

    /// <summary>Tries to read the entry table row a state and glyph class name.</summary>
    /// <typeparam name="T">The entry type the owning table defines. Must be a big-endian, unpadded wire struct that satisfies <see cref="IEndianReversibleStruct{T}"/>.</typeparam>
    /// <param name="state">The state, which is a row of the state array.</param>
    /// <param name="glyphClass">The glyph class, which is a column of the state array.</param>
    /// <param name="entry">When this method returns <see langword="true"/>, contains the entry; otherwise, <c>default</c>.</param>
    /// <returns><see langword="true"/> when a state array element named the entry and it could be read; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">The state table has no <see cref="Source"/> to read from.</exception>
    /// <exception cref="EndOfStreamException">The entry extends past the end of the table-scoped source.</exception>
    /// <remarks>This is the whole transition in one call: classify the glyph with <see cref="GetGlyphClass(int)"/>, pass the current state and that class here, apply the entry's flags, and continue from the state its <see cref="GlyphStateEntry.NewState"/> field names. It fails, without throwing, exactly when <see cref="GetEntryIndex(ushort, ushort)"/> answers <see cref="NoEntryIndex"/>.</remarks>
    /// <seealso cref="ReadEntry{T}(int)"/>
    /// <seealso cref="GetEntryIndex(ushort, ushort)"/>
    public bool TryReadEntry<T>(ushort state, ushort glyphClass, out T entry) where T : unmanaged, IEndianReversibleStruct<T>
    {
        ushort index = GetEntryIndex(state, glyphClass);
        if (index == NoEntryIndex)
        {
            entry = default;
            return false;
        }

        entry = ReadEntry<T>(index);
        return true;
    }

    /// <summary>Reads an extended state table, whose header is the <see cref="StateHeaderExtended"/> structure, at an absolute offset.</summary>
    /// <param name="cursor">The cursor over the table the state table lives in. Only its <see cref="Cursor.Source"/> is used; its position is not changed.</param>
    /// <param name="stateHeaderOffset">The absolute offset of the <see cref="StateHeaderExtended"/> within <paramref name="cursor"/>'s source. Every offset in the header is relative to this byte.</param>
    /// <param name="entrySize">The size, in bytes, of one entry table row for the table that owns this state table. Must be at least <see cref="GlyphStateEntry.SizeInBytes"/>, because every entry begins with a state word and a flags word.</param>
    /// <param name="glyphCount">The font's glyph count, from <c>maxp</c>, used to bound a format 0 class table, or <c>0</c> when no glyph count is known.</param>
    /// <returns>The parsed state table.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="stateHeaderOffset"/> is negative, or <paramref name="entrySize"/> is smaller than the two-word entry prefix.</exception>
    /// <exception cref="InvalidDataException">The header's <c>nClasses</c> field is 0, which leaves the state array with no columns.</exception>
    /// <exception cref="EndOfStreamException">A structure extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The class table is resolved with <see cref="LookupTable.Parse(ref Cursor, object?)"/>, so every lookup format the manual defines is accepted, and <paramref name="glyphCount"/> is passed to it as the context a format 0 lookup needs.</description></item>
    /// <item><description>Neither header form declares how many states the state array holds. The count is therefore derived from the distance between the state array and the entry table, divided by two. That works whenever the entry table follows the state array, which is the layout fonts use, but the manual permits the three pieces to appear in any order: when the entry table lies before the state array, the row count cannot be derived and <see cref="StateArray"/> is empty.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html"><c>morx</c> table</see> for a table built on this header.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // A morx rearrangement subtable's payload starts with the extended state header,
    /// // and its entries are two words wide.
    /// AatStateTable table = AatStateTable.Read(ref cursor, payloadStart, GlyphStateEntry.SizeInBytes, glyphCount);
    /// </code>
    /// </example>
    /// <seealso cref="StateHeaderExtended"/>
    /// <seealso cref="ReadFromShortHeader(ref Cursor, long, StateHeader, int, int)"/>
    /// <seealso cref="LookupTable.Parse(ref Cursor, object?)"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Extended State Tables</seealso>
    public static AatStateTable Read(ref Cursor cursor, long stateHeaderOffset, int entrySize, int glyphCount = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(stateHeaderOffset);
        ThrowIfEntrySizeTooSmall(entrySize);

        Source source = cursor.Source;
        StateHeaderExtended header = source.ReadEndianReversibleStructAt<StateHeaderExtended>(stateHeaderOffset);

        if (header.ClassCount == 0)
        {
            throw new InvalidDataException(
                $"AAT extended state table 'nClasses' is 0 at offset {stateHeaderOffset}, which leaves the state array with no columns.");
        }

        long entryTableStart = stateHeaderOffset + header.EntryTableOffset;

        return new AatStateTable
        {
            ClassCount = header.ClassCount,
            ClassTable = ParseClassTable(source, stateHeaderOffset + header.ClassTableOffset, glyphCount),
            StateArray = ReadStateArray(source, stateHeaderOffset + header.StateArrayOffset, entryTableStart),
            EntrySize = entrySize,
            Source = source,
            EntryTableOffset = entryTableStart,
        };
    }

    /// <summary>Reads a state table whose header is the short <see cref="StateHeader"/> structure, at an absolute offset.</summary>
    /// <param name="cursor">The cursor over the table the state table lives in. Only its <see cref="Cursor.Source"/> is used; its position is not changed.</param>
    /// <param name="stateHeaderOffset">The absolute offset of the state header within <paramref name="cursor"/>'s source. The header's 16-bit offsets are relative to this byte.</param>
    /// <param name="header">The already-read short state header. Its class count field gives the width of one state array row.</param>
    /// <param name="entrySize">The size, in bytes, of one entry table row for the table that owns this state table. Must be at least <see cref="GlyphStateEntry.SizeInBytes"/>.</param>
    /// <param name="glyphCount">The font's glyph count, from <c>maxp</c>, used to bound a format 0 class table, or <c>0</c> when no glyph count is known.</param>
    /// <returns>The parsed state table.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="stateHeaderOffset"/> is negative, or <paramref name="entrySize"/> is smaller than the two-word entry prefix.</exception>
    /// <exception cref="InvalidDataException">The header's class count is 0, which leaves the state array with no columns.</exception>
    /// <exception cref="EndOfStreamException">A structure extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The short header is the original state table header: 16-bit offsets and, in the format the manual describes, an 8-bit state size whose class table is a dense trimmed array. The class table is still resolved as a <see cref="LookupTable"/>, because a format 8 trimmed array lookup yields the same answers as the original class array.</description></item>
    /// <item><description>The header is supplied already read because its caller has to know which form it is looking at, and because the header's own bytes may be followed immediately by a table-specific header word. Read it with <see cref="Cursor.ReadBigEndianStruct{T}"/> first, then pass it here with the offset it was read at.</description></item>
    /// <item><description>The state array row count is derived from the distance to the entry table exactly as in <see cref="Read(ref Cursor, long, int, int)"/>; see that method's remarks for the caveat.</description></item>
    /// <item><description>In this form the entry's new-state word is a byte offset into the state table rather than a row index; see <see cref="GlyphStateEntry.NewState"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="StateHeader"/>
    /// <seealso cref="StateHeaderExtended"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: State Tables</seealso>
    public static AatStateTable ReadFromShortHeader(ref Cursor cursor, long stateHeaderOffset, StateHeader header, int entrySize, int glyphCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(stateHeaderOffset);
        ThrowIfEntrySizeTooSmall(entrySize);

        if (header.GlyphClassCount == 0)
        {
            throw new InvalidDataException(
                $"AAT state table header 'glyphClassCount' is 0 at offset {stateHeaderOffset}, which leaves the state array with no columns.");
        }

        Source source = cursor.Source;
        long entryTableStart = stateHeaderOffset + header.EntryTableOffset;

        return new AatStateTable
        {
            ClassCount = header.GlyphClassCount,
            ClassTable = ParseClassTable(source, stateHeaderOffset + header.ClassTableOffset, glyphCount),
            StateArray = ReadStateArray(source, stateHeaderOffset + header.StateArrayOffset, entryTableStart),
            EntrySize = entrySize,
            Source = source,
            EntryTableOffset = entryTableStart,
        };
    }

    /// <summary>Rejects an entry size that cannot describe a state table entry.</summary>
    /// <param name="entrySize">The caller-supplied entry size, in bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="entrySize"/> is smaller than the two-word entry prefix.</exception>
    private static void ThrowIfEntrySizeTooSmall(int entrySize)
    {
        if (entrySize < GlyphStateEntry.SizeInBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(entrySize),
                entrySize,
                $"An AAT state table entry begins with a state word and a flags word, so entrySize must be at least {GlyphStateEntry.SizeInBytes} bytes.");
        }
    }

    /// <summary>Resolves a class subtable at an absolute offset through the shared lookup table dispatcher.</summary>
    /// <param name="source">The source the class subtable lives in.</param>
    /// <param name="classTableStart">The absolute offset of the class subtable.</param>
    /// <param name="glyphCount">The font's glyph count, or <c>0</c> when it is unknown.</param>
    /// <returns>The parsed class lookup table.</returns>
    /// <exception cref="InvalidDataException">The lookup format is not one the manual defines.</exception>
    /// <exception cref="EndOfStreamException">The lookup extends past the end of the source.</exception>
    private static LookupTable ParseClassTable(Source source, long classTableStart, int glyphCount)
    {
        Cursor lookupCursor = source.CreateCursor(classTableStart);
        object? context = glyphCount > 0 ? (object)glyphCount : null;

        return LookupTable.Parse(ref lookupCursor, context);
    }

    /// <summary>Reads the state array as whole 16-bit entries up to the start of the entry table.</summary>
    /// <param name="source">The source the state array lives in.</param>
    /// <param name="stateArrayStart">The absolute offset of the state array.</param>
    /// <param name="entryTableStart">The absolute offset of the entry table, which bounds the state array.</param>
    /// <returns>The state array, or an empty array when the entry table does not follow the state array.</returns>
    /// <exception cref="EndOfStreamException">The state array extends past the end of the source.</exception>
    private static IReadOnlyList<ushort> ReadStateArray(Source source, long stateArrayStart, long entryTableStart)
    {
        long byteCount = entryTableStart - stateArrayStart;
        if (byteCount < 2) return [];

        return source.ReadUInt16ArrayAt(stateArrayStart, checked((int)(byteCount / 2)));
    }
}
