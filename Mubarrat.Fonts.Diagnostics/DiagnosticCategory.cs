namespace Mubarrat.Fonts.OpenType.Diagnostics;

/// <summary>What kind of rule produced the diagnostic.</summary>
public enum DiagnosticCategory
{
    /// <summary>The bytes could not be read as the spec describes.</summary>
    Structure,

    /// <summary>A field holds a value the spec forbids.</summary>
    Specification,

    /// <summary>Two values disagree — cross-field or cross-table.</summary>
    Consistency,

    /// <summary>Valid but non-standard; other implementations may behave differently.</summary>
    Compatibility,

    /// <summary>Valid but inefficient or redundant.</summary>
    Performance,
}
