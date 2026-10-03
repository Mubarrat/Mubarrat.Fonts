namespace Mubarrat.Fonts.OpenType.Diagnostics;

/// <summary>
/// Severity of a diagnostic, ordered from most to least significant.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>A rule the specification requires. The font is malformed.</summary>
    Error = 0,

    /// <summary>The font is readable but suspicious. Usually a real bug in the font.</summary>
    Warning = 1,

    /// <summary>A notable fact about the font that a tool might surface.</summary>
    Information = 2,

    /// <summary>Detail useful only when diagnosing something else.</summary>
    Verbose = 3,
}
