namespace Mubarrat.Fonts.OpenType.Diagnostics;

/// <summary>Accumulates diagnostics during a single analysis pass.</summary>
public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _items = [];

    /// <summary>Gets the diagnostics collected so far, in the order they were added.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics => _items;

    /// <summary>Gets the number of diagnostics collected.</summary>
    public int Count => _items.Count;

    /// <summary>Adds one diagnostic.</summary>
    public void Add(Diagnostic diagnostic) => _items.Add(diagnostic);

    /// <summary>Adds a sequence of diagnostics.</summary>
    public void AddRange(IEnumerable<Diagnostic> diagnostics) => _items.AddRange(diagnostics);
}
