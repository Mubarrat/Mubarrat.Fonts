namespace Mubarrat.Fonts.OpenType.Diagnostics;

/// <summary>
/// Runs the built-in rule set and any consumer-supplied analyzers against a
/// <see cref="FontFace"/>, returning the resulting diagnostics.
/// </summary>
/// <remarks>
/// The built-in table list is hard-coded. This preserves the library's no-registry,
/// no-reflection design: adding a rule for a table means adding one line here.
/// Consumers extend the rule set by implementing <see cref="IFontAnalyzer"/> and
/// calling <see cref="Add"/>.
/// </remarks>
public sealed class FontAnalyzer
{
    private readonly AnalysisOptions _options;
    private readonly List<IFontAnalyzer> _additional = [];

    /// <summary>Creates an analyzer with the given options, or defaults if <c>null</c>.</summary>
    public FontAnalyzer(AnalysisOptions? options = null) =>
        _options = options ?? new();

    /// <summary>Gets the options applied to each run.</summary>
    public AnalysisOptions Options => _options;

    /// <summary>Registers a consumer-supplied analyzer. Runs after the built-in rules.</summary>
    public void Add(IFontAnalyzer analyzer)
    {
        ArgumentNullException.ThrowIfNull(analyzer);
        _additional.Add(analyzer);
    }

    /// <summary>Runs every rule against <paramref name="face"/> and returns the diagnostics.</summary>
    public IReadOnlyList<Diagnostic> Analyze(FontFace face)
    {
        ArgumentNullException.ThrowIfNull(face);

        var bag = new DiagnosticBag();

        // Consumer-supplied rules.
        foreach (var analyzer in _additional)
            analyzer.Analyze(face, bag);

        return Filter(bag.Diagnostics);
    }

    private IReadOnlyList<Diagnostic> Filter(IReadOnlyList<Diagnostic> diagnostics)
    {
        if (_options.Overrides.Any() is false) return diagnostics;

        var result = new List<Diagnostic>(diagnostics.Count);
        foreach (var d in diagnostics)
        {
            var effective = _options.Apply(d.Id, d.Severity);
            if (effective is null) continue;   // suppressed
            result.Add(d.Severity == effective ? d : d with { Severity = effective.Value });
        }
        return result;
    }
}
