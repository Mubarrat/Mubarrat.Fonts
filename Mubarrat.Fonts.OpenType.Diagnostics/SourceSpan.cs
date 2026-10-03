namespace Mubarrat.Fonts.OpenType.Diagnostics;

/// <summary>A byte range within a source. Offset is zero-based.</summary>
public readonly record struct SourceSpan(long Offset, int Length)
{
    /// <summary>Gets the byte offset one past the end of the range.</summary>
    public long End => Offset + Length;

    public override string ToString() => $"0x{Offset:X}+{Length}";
}
