using System.Globalization;
using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType.Diagnostics;

/// <summary>
/// A single occurrence of a <see cref="DiagnosticDescriptor"/>: where it happened and
/// what values to substitute into the message format.
/// </summary>
/// <param name="Descriptor">The rule this occurrence belongs to.</param>
/// <param name="Severity">
/// Effective severity. Normally equal to <see cref="DiagnosticDescriptor.DefaultSeverity"/>;
/// differs only after a severity override is applied.
/// </param>
/// <param name="Arguments">Values substituted into the descriptor's message format.</param>
/// <param name="Table">The table involved, or <c>null</c> when the diagnostic is table-independent.</param>
/// <param name="Field">The field name within the table, or <c>null</c>.</param>
/// <param name="Span">The byte range in the source, or <c>null</c> when not available.</param>
public sealed record Diagnostic(
    DiagnosticDescriptor Descriptor,
    DiagnosticSeverity Severity,
    object?[] Arguments,
    Tag? Table = null,
    string? Field = null,
    SourceSpan? Span = null)
{
    /// <summary>Gets the rule's stable identifier.</summary>
    public string Id => Descriptor.Id;

    /// <summary>Gets the formatted message. Recomputed on each access.</summary>
    public string Message =>
        string.Format(CultureInfo.InvariantCulture, Descriptor.MessageFormat, Arguments);

    public override string ToString()
    {
        var location = (Table, Field) switch
        {
            (null, null) => null,
            (not null, null) => $"'{Table}'",
            (null, not null) => Field,
            (not null, not null) => $"'{Table}'.{Field}",
        };

        var prefix = Span is { } s ? $"[{s}] " : "";
        var where = location is null ? "" : $" {location}";
        return $"{prefix}{Severity} {Id}:{where} {Message}";
    }
}
