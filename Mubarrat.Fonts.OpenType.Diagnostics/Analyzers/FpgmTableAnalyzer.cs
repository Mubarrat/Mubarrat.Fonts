using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables;
using Mubarrat.Fonts.OpenType.Tables.Hinting;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>fpgm</c> table.</summary>
/// <remarks>
/// <para>
/// The parser decodes the bytecode into a structural instruction list. What remains is
/// interpretation of the program's structure: unknown opcodes, function definition
/// balance, and the relationship between the definitions here and the limits declared in
/// <c>maxp</c>.
/// </para>
/// <para>
/// Every FDEF in fpgm increments the effective function count. The highest function number
/// seen must be less than <c>maxp.maxFunctionDefs</c>, and the count of definitions must
/// not exceed it.
/// </para>
/// </remarks>
public class FpgmTableAnalyzer : IFontAnalyzer
{
    public static readonly DiagnosticDescriptor UnknownOpcode = new(
        "OT.fpgm.unknown-opcode", "Program contains an unrecognized opcode",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Instruction at index {0} uses opcode 0x{1:X2}, which is not a defined TrueType instruction.",
        "Reserved opcode ranges are not defined by the specification. Their presence usually indicates a malformed program or a future extension.");

    public static readonly DiagnosticDescriptor UnbalancedFunctions = new(
        "OT.fpgm.unbalanced-functions", "Function definitions are not balanced",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "The program has {0} FDEF and {1} ENDF instructions.",
        "Every FDEF must be closed by a matching ENDF. Unbalanced counts mean a function definition runs into the next one, or off the end of the program.");

    public static readonly DiagnosticDescriptor FunctionCountExceedsMaxp = new(
        "OT.fpgm.function-count-vs-maxp", "Function count exceeds maxp.maxFunctionDefs",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "The program defines {0} functions but maxp.maxFunctionDefs is {1}.",
        "Every function defined by FDEF consumes a slot in the maxp budget. A definition beyond the declared limit means the font is malformed.");

    public static readonly DiagnosticDescriptor UndefinedFunctionDef = new(
        "OT.fpgm.undefined-function-def", "FDEF missing its function number",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Structure,
        "The FDEF at instruction index {0} has no preceding push instruction supplying a function number.",
        "FDEF reads its function number from the operand stack. A missing push means the interpreter will use whatever the stack happens to hold.");

    public static readonly DiagnosticDescriptor Empty = new(
        "OT.fpgm.empty", "fpgm table is empty",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "The 'fpgm' table contains no instructions.",
        "A font that declares no function definitions should omit the fpgm table entirely.");

    public static readonly DiagnosticDescriptor NoFunctionDefinitions = new(
        "OT.fpgm.no-fdefs", "Program declares no functions",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Consistency,
        "The program contains no FDEF instructions.",
        "The fpgm table exists to define functions. A program without FDEFs provides no information.");

    public static readonly DiagnosticDescriptor NestedFdef = new(
        "OT.fpgm.nested-fdef", "FDEF appears inside another FDEF",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Structure,
        "The FDEF at instruction index {0} appears before the previous function's ENDF.",
        "Function definitions cannot be nested. A nested FDEF means the previous function was never terminated.");

    public static readonly DiagnosticDescriptor InstructionCountHigh = new(
        "OT.fpgm.instruction-count", "Program is unusually large",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Performance,
        "The fpgm program has {0} instructions.",
        "Large fpgm programs increase the cost of the initial hinting pass. Informational; not a defect on its own.");

    private FpgmTableAnalyzer() { }

    public static FpgmTableAnalyzer Instance => field ??= new();

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var fpgm = face.GetTable<FpgmTable>();
        var tag = FpgmTable.Tag;

        if (fpgm.Program.Count == 0)
        {
            bag.Add(Empty.Create([], table: tag, field: nameof(FpgmTable.Program)));
            return;
        }

        AnalyzeInstructions(fpgm, tag, bag);
        AnalyzeFunctionBudget(face, fpgm, tag, bag);

        if (fpgm.InstructionCount > 5000)
        {
            bag.Add(InstructionCountHigh.Create(
                [fpgm.InstructionCount], table: tag));
        }
    }

    private static void AnalyzeInstructions(FpgmTable fpgm, Tag tag, DiagnosticBag bag)
    {
        var instructions = fpgm.Program.Instructions;

        int depth = 0;
        int fdefCount = 0;
        int endfCount = 0;
        int maxFunctionNumber = -1;

        for (int i = 0; i < instructions.Count; i++)
        {
            TrueTypeInstruction ins = instructions[i];

            if (ins.Opcode == TrueTypeOpcode.Unknown)
            {
                bag.Add(UnknownOpcode.Create(
                    [i, ins.RawOpcode],
                    table: tag, field: $"program.instructions[{i}]"));
            }

            if (ins.Opcode == TrueTypeOpcode.Fdef)
            {
                if (depth > 0)
                {
                    bag.Add(NestedFdef.Create(
                        [i], table: tag, field: $"program.instructions[{i}]"));
                }

                fdefCount++;
                depth++;

                // Find the nearest preceding push; its last operand is the function number.
                int fnum = FindPrecedingFunctionNumber(instructions, i);
                if (fnum < 0)
                {
                    bag.Add(UndefinedFunctionDef.Create(
                        [i], table: tag, field: $"program.instructions[{i}]"));
                }
                else if (fnum > maxFunctionNumber)
                {
                    maxFunctionNumber = fnum;
                }
            }
            else if (ins.Opcode == TrueTypeOpcode.Endf)
            {
                endfCount++;
                if (depth > 0) depth--;
            }
        }

        if (fdefCount != endfCount)
        {
            bag.Add(UnbalancedFunctions.Create(
                [fdefCount, endfCount], table: tag));
        }

        if (fdefCount == 0)
        {
            bag.Add(NoFunctionDefinitions.Create([], table: tag));
        }
    }

    /// <summary>
    /// Walks backward from a FDEF to the most recent push instruction and returns the last
    /// operand (the function number), or -1 when no push precedes it.
    /// </summary>
    private static int FindPrecedingFunctionNumber(
        IReadOnlyList<TrueTypeInstruction> instructions, int fdefIndex)
    {
        for (int j = fdefIndex - 1; j >= 0; j--)
        {
            TrueTypeInstruction prev = instructions[j];
            if (prev.IsPush)
                return prev.InlineData[^1];

            // A non-push instruction between the push and the FDEF means the value is
            // whatever was on the stack at that point; the interpreter alone knows.
            if (prev.Opcode != TrueTypeOpcode.NPushB &&
                prev.Opcode != TrueTypeOpcode.NPushW &&
                prev.Opcode != TrueTypeOpcode.PushB &&
                prev.Opcode != TrueTypeOpcode.PushW)
            {
                return -1;
            }
        }
        return -1;
    }

    private static void AnalyzeFunctionBudget(FontFace face, FpgmTable fpgm, Tag tag, DiagnosticBag bag)
    {
        if (!face.Directory.Contains(MaxpTable.Tag)) return;

        var maxp = face.GetTable<MaxpTable>();
        int? declared = maxp.MaxFunctionDefs;
        if (declared is null) return;

        int defined = CountFunctions(fpgm.Program.Instructions);
        if (defined > declared)
        {
            bag.Add(FunctionCountExceedsMaxp.Create(
                [defined, declared],
                table: tag, field: nameof(FpgmTable.Program)));
        }
    }

    private static int CountFunctions(IReadOnlyList<TrueTypeInstruction> instructions)
    {
        int count = 0;
        foreach (var ins in instructions)
            if (ins.Opcode == TrueTypeOpcode.Fdef) count++;
        return count;
    }
}
