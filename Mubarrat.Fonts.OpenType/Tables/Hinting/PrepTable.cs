using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables.Outlines;

namespace Mubarrat.Fonts.OpenType.Tables.Hinting;

/// <summary>The <c>prep</c> table: Control Value Program. A TrueType instruction program executed whenever the font, point size, or transformation matrix changes, and before each glyph is interpreted.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The bytecode is decoded into a <see cref="TrueTypeProgram"/>, so consumers can inspect or interpret the instructions directly.</description></item>
/// <item><description>The raw bytes are preserved on the program and remain accessible through <see cref="TrueTypeProgram"/>.</description></item>
/// <item><description>The <c>prep</c> program typically calls functions defined by <c>fpgm</c>, adjusts control values that were read from <c>cvt </c>, and sets up state that the per-glyph instruction streams rely on.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/prep">prep table</see> chapter in the OpenType specification.</description></item>
/// <item><description>For the instruction set itself, see the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/tt_instructions">TrueType instruction set</see>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="TrueTypeProgram"/>
/// <seealso cref="FpgmTable"/>
/// <seealso cref="CvtTable"/>
/// <seealso cref="GlyfTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/prep">OpenType specification: prep table</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/tt_instructions">OpenType specification: TrueType instruction set</seealso>
public sealed record PrepTable : IOpenTypeTable<PrepTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IOpenTypeTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/prep">OpenType specification: prep table</seealso>
    public static Tag Tag => "prep";

    /// <summary>Gets the decoded program.</summary>
    /// <value>The <see cref="TrueTypeProgram"/> containing the decoded instructions and the raw bytecode they were decoded from.</value>
    /// <seealso cref="InstructionCount"/>
    /// <seealso cref="TrueTypeProgram"/>
    public TrueTypeProgram Program { get; init; } = null!;

    /// <summary>Gets the number of decoded instructions.</summary>
    /// <value>The instruction count reported by <see cref="TrueTypeProgram.Count"/>.</value>
    /// <seealso cref="Program"/>
    public int InstructionCount => Program.Count;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the prep table.</param>
    /// <param name="context">Unused. The prep table is self-contained.</param>
    /// <returns>The parsed prep table with its decoded program.</returns>
    /// <exception cref="EndOfStreamException">The program extends past the end of the table-scoped source.</exception>
    /// <exception cref="InvalidDataException">The bytecode is structurally invalid — for example, a truncated instruction opcode.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The program length is taken from the table-scoped cursor's <see cref="Cursor.Length"/>, so the reader consumes exactly the bytes the table declares.</description></item>
    /// <item><description>The bytecode is decoded eagerly; the raw bytes remain on the returned <see cref="TrueTypeProgram"/> for callers that need them.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/prep">prep table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="TrueTypeProgram"/>
    /// <seealso cref="TrueTypeProgramContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/prep">OpenType specification: prep table</seealso>
    static PrepTable IRecord<PrepTable>.Parse(ref Cursor cursor, object? context) => new() { Program = cursor.ReadRecord<TrueTypeProgram>(new TrueTypeProgramContext((int)cursor.Length)) };
}
