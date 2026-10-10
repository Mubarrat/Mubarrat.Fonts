using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>prep</c> table.</summary>
/// <remarks>
/// <para>
/// The prep program runs once per size and may define instruction definitions (IDEFs) that
/// the glyf programs call. It must not define functions — FDEF is reserved for fpgm.
/// </para>
/// <para>
/// The analyzer checks the structural shape of the program, the IDEF budget against
/// <c>maxp.maxInstructionDefs</c>, and the balance of the storage writes against
/// <c>maxp.maxStorage</c>.
/// </para>
/// </remarks>
public class PrepTableAnalyzer : IFontAnalyzer
{
    public static readonly DiagnosticDescriptor UnknownOpcode = new(
        "OT.prep.unknown-opcode", "Program contains an unrecognized opcode",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Instruction at index {0} uses opcode 0x{1:X2}, which is not a defined TrueType instruction.",
        "Reserved opcode ranges are not defined by the specification. Their presence usually indicates a malformed program or a future extension.");

    public static readonly DiagnosticDescriptor FunctionDefinitionInPrep = new(
        "OT.prep.fdef-not-allowed", "FDEF appears in prep",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "Instruction at index {0} is FDEF; function definitions belong in fpgm, not prep.",
        "FDEF is only permitted in the fpgm table. A prep program containing FDEF is non-conforming.");

    public static readonly DiagnosticDescriptor UnbalancedInstructionDefs = new(
        "OT.prep.unbalanced-idefs", "Instruction definitions are not balanced",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "The program has {0} IDEF and {1} ENDF instructions.",
        "Every IDEF must be closed by a matching ENDF. Unbalanced counts mean an instruction definition runs into the next one or off the end of the program.");

    public static readonly DiagnosticDescriptor InstructionDefCountExceedsMaxp = new(
        "OT.prep.instruction-def-count-vs-maxp", "Instruction definition count exceeds maxp.maxInstructionDefs",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "The program defines {0} instructions but maxp.maxInstructionDefs is {1}.",
        "Every IDEF consumes a slot in the maxp budget. A definition beyond the declared limit means the font is malformed.");

    public static readonly DiagnosticDescriptor StorageExceedsMaxp = new(
        "OT.prep.storage-vs-maxp", "Prep writes storage beyond maxp.maxStorage",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "The program writes storage index {0} but maxp.maxStorage is {1}.",
        "WS and RS write and read the storage area. Every index used must fall within maxp.maxStorage.");

    public static readonly DiagnosticDescriptor Empty = new(
        "OT.prep.empty", "prep table is empty",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "The 'prep' table contains no instructions.",
        "A font that needs no per-size preparation should omit the prep table entirely.");

    private PrepTableAnalyzer() { }

    public static PrepTableAnalyzer Instance => field ??= new();

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var prep = face.GetTable<PrepTable>();
        var tag = PrepTable.Tag;

        if (prep.Program.Count == 0)
        {
            bag.Add(Empty.Create([], table: tag, field: nameof(PrepTable.Program)));
            return;
        }

        AnalyzeInstructions(prep, tag, bag);
        AnalyzeBudgets(face, prep, tag, bag);
    }

    private static void AnalyzeInstructions(PrepTable prep, Tag tag, DiagnosticBag bag)
    {
        var instructions = prep.Program.Instructions;

        int depth = 0;
        int idefCount = 0;
        int endfCount = 0;

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
                bag.Add(FunctionDefinitionInPrep.Create(
                    [i], table: tag, field: $"program.instructions[{i}]"));
            }

            if (ins.Opcode == TrueTypeOpcode.Idef)
            {
                idefCount++;
                depth++;
            }
            else if (ins.Opcode == TrueTypeOpcode.Endf)
            {
                endfCount++;
                if (depth > 0) depth--;
            }
        }

        if (idefCount != endfCount)
        {
            bag.Add(UnbalancedInstructionDefs.Create(
                [idefCount, endfCount], table: tag));
        }
    }

    private static void AnalyzeBudgets(FontFace face, PrepTable prep, Tag tag, DiagnosticBag bag)
    {
        if (!face.Directory.ContainsKey(MaxpTable.Tag)) return;

        var maxp = face.GetTable<MaxpTable>();

        int? maxInstr = maxp.MaxInstructionDefs;
        if (maxInstr is { } instrBudget)
        {
            int defined = CountOpcode(prep.Program.Instructions, TrueTypeOpcode.Idef);
            if (defined > instrBudget)
            {
                bag.Add(InstructionDefCountExceedsMaxp.Create(
                    [defined, instrBudget],
                    table: tag, field: nameof(PrepTable.Program)));
            }
        }

        int? maxStorage = maxp.MaxStorage;
        if (maxStorage is { } storageBudget)
        {
            int highest = HighestStorageIndex(prep.Program.Instructions);
            if (highest >= storageBudget)
            {
                bag.Add(StorageExceedsMaxp.Create(
                    [highest, storageBudget],
                    table: tag, field: nameof(PrepTable.Program)));
            }
        }
    }

    private static int CountOpcode(IReadOnlyList<TrueTypeInstruction> instructions, TrueTypeOpcode opcode)
    {
        int count = 0;
        foreach (var ins in instructions)
            if (ins.Opcode == opcode) count++;
        return count;
    }

    /// <summary>
    /// Finds the highest storage index referenced by WS or RS. WS takes (value, location);
    /// RS takes (location). The location is on top of the stack in both cases.
    /// </summary>
    private static int HighestStorageIndex(IReadOnlyList<TrueTypeInstruction> instructions)
    {
        int highest = -1;
        for (int i = 0; i < instructions.Count; i++)
        {
            TrueTypeInstruction ins = instructions[i];
            if (ins.Opcode != TrueTypeOpcode.Ws && ins.Opcode != TrueTypeOpcode.Rs) continue;

            // Find the most recent push(es) that supply the location operand.
            // WS consumes two values; RS consumes one.
            int needed = ins.Opcode == TrueTypeOpcode.Ws ? 2 : 1;
            int found = 0;
            for (int j = i - 1; j >= 0 && found < needed; j--)
            {
                TrueTypeInstruction prev = instructions[j];
                if (!prev.IsPush) continue;

                int available = prev.InlineData.Length;
                if (found == 0)
                {
                    // Top of stack is the last element of the most recent push.
                    int location = prev.InlineData[available - 1];
                    if (location > highest) highest = location;
                }
                found += available;
            }
        }
        return highest;
    }
}
