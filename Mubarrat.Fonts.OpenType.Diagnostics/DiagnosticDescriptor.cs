using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.OpenType.Diagnostics;

/// <summary>
/// Identifies a single analysis rule: its ID, its default severity, and the format of
/// its message. One descriptor is defined per rule and shared by every occurrence.
/// </summary>
/// <param name="Id">
/// Stable rule identifier. Convention: <c>OT</c> + four digits, where the first digit
/// is the category (<c>OT2xxx</c> for global metrics, <c>OT3xxx</c> for cmap, and so on).
/// IDs are never reused, even after a rule is retired.
/// </param>
/// <param name="Title">Short human-readable summary, shown in rule listings.</param>
/// <param name="DefaultSeverity">Severity unless overridden by <see cref="AnalysisOptions"/>.</param>
/// <param name="Category">What kind of rule this is.</param>
/// <param name="MessageFormat">
/// Message template. Uses standard <see cref="string.Format(IFormatProvider, string, object?)"/>
/// placeholders (<c>{0}</c>, <c>{1}</c>, …). The number of placeholders must match the
/// number of arguments passed to <see cref="Create"/>.
/// </param>
/// <param name="Description">
/// Optional long-form explanation, for documentation or a tooltip. Not part of the message.
/// </param>
public sealed record DiagnosticDescriptor(
    string Id,
    string Title,
    DiagnosticSeverity DefaultSeverity,
    DiagnosticCategory Category,
    string MessageFormat,
    string? Description = null)
{
    /// <summary>
    /// Creates an occurrence of this rule with the descriptor's default severity.
    /// </summary>
    public Diagnostic Create(
        object?[] arguments,
        Tag? table = null,
        string? field = null,
        SourceSpan? span = null) =>
        new(this, DefaultSeverity, arguments, table, field, span);

    public override string ToString() => $"{Id}: {Title}";
}
