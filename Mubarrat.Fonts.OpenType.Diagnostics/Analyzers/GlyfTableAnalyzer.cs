using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>glyf</c> table.</summary>
/// <remarks>
/// <para>
/// <c>glyf</c> is the single largest table in a TrueType font and the one whose structural
/// invariants are easiest to violate during font editing. The parser already rejects
/// truncated blocks and non-monotonic <c>loca</c> ranges at read time; the rules here
/// cover the composite graph — reference validity, cycles, contradictory flag
/// combinations — and the semantic shape of individual glyphs.
/// </para>
/// <para>
/// Rules that compare <c>glyf</c> against <c>loca</c> (offset array shape) or
/// <c>maxp</c> (glyph count) fire on <c>glyf</c> because the outline data is what must
/// fit the declared glyph inventory, not the reverse.
/// </para>
/// </remarks>
public class GlyfTableAnalyzer : IFontAnalyzer
{
    // ─────────────────────── Constants ───────────────────────

    /// <summary>Reserved bits in <see cref="CompositeGlyphFlags"/>: bit 4 and bits 13–15.</summary>
    private const ushort ComponentReservedFlagsConst = 0xE010;

    // ─────────────────────── Descriptors ───────────────────────

    public static readonly DiagnosticDescriptor GlyphCountMismatch = new(
        "OT.glyf.glyph-count-vs-maxp", "Glyph count disagrees with maxp",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "glyf has {0} glyphs but maxp.numGlyphs is {1}.",
        "Every glyph in the font must have an entry in glyf. A mismatch means the font's glyph inventory and its outline data disagree.");

    public static readonly DiagnosticDescriptor ComponentGlyphIndexOutOfRange = new(
        "OT.glyf.component-glyph-out-of-range", "Component references a non-existent glyph",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "Glyph {0} component {1} references glyph {2}, but the font has only {3} glyphs.",
        "Every component reference must point at a valid glyph ID within [0, numGlyphs).");

    public static readonly DiagnosticDescriptor ComponentCycle = new(
        "OT.glyf.component-cycle", "Composite glyph graph contains a cycle",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "Glyph {0} participates in a component cycle.",
        "The composite glyph graph must be a directed acyclic graph. A cycle causes infinite recursion in every rasterizer that walks the graph.");

    public static readonly DiagnosticDescriptor BboxInverted = new(
        "OT.glyf.bbox-inverted", "Glyph bounding box is inverted",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "Glyph {0} bounding box is inverted: ({1}, {2}) – ({3}, {4}).",
        "An inverted bounding box is a common symptom of a font that was edited without re-computing the glyph metrics.");

    public static readonly DiagnosticDescriptor ComponentConflictingTransformFlags = new(
        "OT.glyf.component-transform-flags", "Component sets multiple transform forms",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "Glyph {0} component {1} has flags 0x{2:X4} with {3} of WE_HAVE_A_SCALE, WE_HAVE_AN_X_AND_Y_SCALE, and WE_HAVE_A_TWO_BY_TWO set.",
        "At most one transform form may be declared. The parser reads only the first one it encounters, so a component with two flags reads the wrong bytes for the matrix.");

    public static readonly DiagnosticDescriptor ComponentConflictingOffsetMode = new(
        "OT.glyf.component-offset-mode", "Component sets both offset modes",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "Glyph {0} component {1} has both SCALED_COMPONENT_OFFSET and UNSCALED_COMPONENT_OFFSET set.",
        "The two flags select mutually exclusive behaviors for the offset vector's coordinate system. Setting both is undefined.");

    public static readonly DiagnosticDescriptor ComponentRoundXYWithoutXY = new(
        "OT.glyf.component-round-xy", "ROUND_XY_TO_GRID set without XY arguments",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Glyph {0} component {1} has ROUND_XY_TO_GRID set but ARGS_ARE_XY_VALUES clear.",
        "ROUND_XY_TO_GRID only applies when the arguments are xy offsets. With point-number arguments, the flag has no effect.");

    public static readonly DiagnosticDescriptor ComponentReservedFlags = new(
        "OT.glyf.component-reserved-flags", "Component has reserved flag bits set",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Glyph {0} component {1} has reserved bits 0x{2:X4} set (full flags 0x{3:X4}).",
        "Bits 4 and 13–15 of a composite glyph component's flags are reserved and must be zero.");

    public static readonly DiagnosticDescriptor ContourNegativePointCount = new(
        "OT.glyf.contour-negative-point-count", "Contour has a negative point count",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Structure,
        "Glyph {0} contour {1} has a point count of {2}.",
        "A negative point count means the contour's endpoint precedes its start, which can only arise from a corrupted endPtsOfContours array.");

    // ─────────────────────── Singleton ───────────────────────

    private GlyfTableAnalyzer() { }

    public static GlyfTableAnalyzer Instance => field ??= new();

    // ─────────────────────── Analysis ───────────────────────

    /// <summary>Runs every glyf rule against the face's <c>glyf</c> table.</summary>
    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var glyf = face.GetTable<GlyfTable>();
        var tag = GlyfTable.Tag;

        AnalyzeCount(face, glyf, tag, bag);
        AnalyzeBoundingBoxes(glyf, tag, bag);
        AnalyzeContours(glyf, tag, bag);
        AnalyzeComponents(glyf, tag, bag);
        AnalyzeCycles(glyf, tag, bag);
    }

    // ─────────────────────── Rule groups ───────────────────────

    private static void AnalyzeCount(FontFace face, GlyfTable glyf, Tag tag, DiagnosticBag bag)
    {
        if (!face.Directory.ContainsKey(MaxpTable.Tag)) return;

        var maxp = face.GetTable<MaxpTable>();
        if (glyf.Count != maxp.NumGlyphs)
        {
            bag.Add(GlyphCountMismatch.Create(
                [glyf.Count, maxp.NumGlyphs],
                table: tag, field: nameof(GlyfTable.Glyphs)));
        }
    }

    private static void AnalyzeBoundingBoxes(GlyfTable glyf, Tag tag, DiagnosticBag bag)
    {
        Glyph[] glyphs = glyf.Glyphs;
        for (int gid = 0; gid < glyphs.Length; gid++)
        {
            Glyph glyph = glyphs[gid];
            if (glyph is EmptyGlyph) continue;

            if (glyph.XMin > glyph.XMax || glyph.YMin > glyph.YMax)
            {
                bag.Add(BboxInverted.Create(
                    [gid, glyph.XMin, glyph.YMin, glyph.XMax, glyph.YMax],
                    table: tag, field: $"glyphs[{gid}].bbox"));
            }
        }
    }

    private static void AnalyzeContours(GlyfTable glyf, Tag tag, DiagnosticBag bag)
    {
        Glyph[] glyphs = glyf.Glyphs;
        for (int gid = 0; gid < glyphs.Length; gid++)
        {
            if (glyphs[gid] is not SimpleGlyph simple) continue;

            Contour[] contours = simple.Contours;
            for (int c = 0; c < contours.Length; c++)
            {
                if (contours[c].PointCount < 0)
                {
                    bag.Add(ContourNegativePointCount.Create(
                        [gid, c, contours[c].PointCount],
                        table: tag, field: $"glyphs[{gid}].contours[{c}]"));
                }
            }
        }
    }

    private static void AnalyzeComponents(GlyfTable glyf, Tag tag, DiagnosticBag bag)
    {
        int numGlyphs = glyf.Count;
        Glyph[] glyphs = glyf.Glyphs;

        for (int gid = 0; gid < numGlyphs; gid++)
        {
            if (glyphs[gid] is not CompositeGlyph composite) continue;

            Component[] components = composite.Components;
            for (int c = 0; c < components.Length; c++)
            {
                Component component = components[c];
                ushort rawFlags = (ushort)component.Flags;

                if (component.GlyphIndex >= numGlyphs)
                {
                    bag.Add(ComponentGlyphIndexOutOfRange.Create(
                        [gid, c, component.GlyphIndex, numGlyphs],
                        table: tag, field: $"glyphs[{gid}].components[{c}]"));
                }

                int transformForms =
                    ((component.Flags & CompositeGlyphFlags.WeHaveAScale) != 0 ? 1 : 0) +
                    ((component.Flags & CompositeGlyphFlags.WeHaveAnXAndYScale) != 0 ? 1 : 0) +
                    ((component.Flags & CompositeGlyphFlags.WeHaveATwoByTwo) != 0 ? 1 : 0);

                if (transformForms > 1)
                {
                    bag.Add(ComponentConflictingTransformFlags.Create(
                        [gid, c, rawFlags, transformForms],
                        table: tag, field: $"glyphs[{gid}].components[{c}]"));
                }

                bool scaled = (component.Flags & CompositeGlyphFlags.ScaledComponentOffset) != 0;
                bool unscaled = (component.Flags & CompositeGlyphFlags.UnscaledComponentOffset) != 0;
                if (scaled && unscaled)
                {
                    bag.Add(ComponentConflictingOffsetMode.Create(
                        [gid, c],
                        table: tag, field: $"glyphs[{gid}].components[{c}]"));
                }

                bool roundXY = (component.Flags & CompositeGlyphFlags.RoundXYToGrid) != 0;
                if (roundXY && !component.ArgsAreXYValues)
                {
                    bag.Add(ComponentRoundXYWithoutXY.Create(
                        [gid, c],
                        table: tag, field: $"glyphs[{gid}].components[{c}]"));
                }

                ushort reserved = (ushort)(rawFlags & ComponentReservedFlagsConst);
                if (reserved != 0)
                {
                    bag.Add(ComponentReservedFlags.Create(
                        [gid, c, reserved, rawFlags],
                        table: tag, field: $"glyphs[{gid}].components[{c}]"));
                }
            }
        }
    }

    private static void AnalyzeCycles(GlyfTable glyf, Tag tag, DiagnosticBag bag)
    {
        int numGlyphs = glyf.Count;
        var state = new VisitState[numGlyphs];
        Glyph[] glyphs = glyf.Glyphs;

        for (int gid = 0; gid < numGlyphs; gid++)
        {
            if (state[gid] != VisitState.Unvisited) continue;
            if (HasCycle(glyphs, gid, state))
            {
                bag.Add(ComponentCycle.Create(
                    [gid],
                    table: tag, field: $"glyphs[{gid}]"));
                return;   // one diagnostic is enough; the graph is broken
            }
        }
    }

    private enum VisitState : byte
    {
        Unvisited,
        InProgress,
        Done,
    }

    /// <summary>
    /// Depth-first search for a cycle reachable from <paramref name="glyphId"/>. Uses the
    /// classic three-color scheme: a glyph already marked <see cref="VisitState.Done"/>
    /// has no cycle beneath it, and one encountered while <see cref="VisitState.InProgress"/>
    /// is on the current stack, which means the edge closes a cycle.
    /// </summary>
    private static bool HasCycle(Glyph[] glyphs, int glyphId, VisitState[] state)
    {
        if (state[glyphId] == VisitState.Done) return false;
        if (state[glyphId] == VisitState.InProgress) return true;

        state[glyphId] = VisitState.InProgress;

        if (glyphs[glyphId] is CompositeGlyph composite)
        {
            foreach (Component component in composite.Components)
            {
                if (component.GlyphIndex >= glyphs.Length) continue;   // reported separately
                if (HasCycle(glyphs, component.GlyphIndex, state)) return true;
            }
        }

        state[glyphId] = VisitState.Done;
        return false;
    }
}
