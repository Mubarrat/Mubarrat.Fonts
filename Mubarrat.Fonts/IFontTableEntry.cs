using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts;

/// <summary>
/// Represents a font table entry that can be stored in a font table directory.
/// </summary>
public interface IFontTableEntry
{
    /// <summary>
    /// Gets the 4-byte tag identifying the font table.
    /// </summary>
    Tag Tag { get; }
}
