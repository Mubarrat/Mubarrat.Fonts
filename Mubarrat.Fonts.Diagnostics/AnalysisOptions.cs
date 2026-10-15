namespace Mubarrat.Fonts.OpenType.Diagnostics;

/// <summary>
/// Per-rule severity overrides for an analysis run. A rule not listed keeps its
/// descriptor's default severity.
/// </summary>
public sealed class AnalysisOptions
{
    private readonly Dictionary<string, DiagnosticSeverity?> _overrides =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Sets the effective severity for <paramref name="ruleId"/>. Pass <c>null</c> to
    /// suppress the rule entirely.
    /// </summary>
    public void SetSeverity(string ruleId, DiagnosticSeverity? severity) =>
        _overrides[ruleId] = severity;

    /// <summary>Restores <paramref name="ruleId"/> to its descriptor's default severity.</summary>
    public void Reset(string ruleId) => _overrides.Remove(ruleId);

    /// <summary>Removes every override.</summary>
    public void ResetAll() => _overrides.Clear();

    /// <summary>Enumerates the current overrides.</summary>
    public IEnumerable<KeyValuePair<string, DiagnosticSeverity?>> Overrides => _overrides;

    /// <summary>
    /// Returns the effective severity for <paramref name="ruleId"/>, or <paramref name="defaultSeverity"/>
    /// when the rule has no override.
    /// </summary>
    public DiagnosticSeverity? Apply(string ruleId, DiagnosticSeverity defaultSeverity) =>
        _overrides.TryGetValue(ruleId, out var s) ? s : defaultSeverity;
}
