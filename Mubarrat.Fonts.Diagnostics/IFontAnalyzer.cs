namespace Mubarrat.Fonts.OpenType.Diagnostics;

/// <summary>
/// A rule set that inspects a parsed font and contributes diagnostics to a bag.
/// Implementations are stateless with respect to the analysis pass; any state they
/// carry is configuration, not per-font.
/// </summary>
public interface IFontAnalyzer
{
    /// <summary>Adds every diagnostic this analyzer finds to <paramref name="bag"/>.</summary>
    /// <param name="face">The font face to analyze.</param>
    /// <param name="bag">The diagnostic bag to add diagnostics to.</param>
    void Analyze(FontFace face, DiagnosticBag bag);
}
