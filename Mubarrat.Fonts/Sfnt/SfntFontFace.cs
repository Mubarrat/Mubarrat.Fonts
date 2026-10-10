using System.Collections.Concurrent;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Sfnt;

/// <summary>
/// Represents a font face stored using the SFNT font-file structure.
/// </summary>
/// <remarks>
/// <para>
/// An SFNT font face consists of a byte source and a table directory.
/// Individual tables are parsed when first requested and cached for the
/// lifetime of this face.
/// </para>
/// <para>
/// Table lookup is type-directed: the requested table type supplies its
/// SFNT tag through <see cref="IFontTable{T}.Tag"/>. No central
/// tag-to-type registry is required.
/// </para>
/// </remarks>
/// <seealso cref="FontFace"/>
/// <seealso cref="SfntTableDirectory"/>
public sealed class SfntFontFace : FontFace, IRecord<SfntFontFace>
{
    private readonly ConcurrentDictionary<Tag, IRecord> _cache = new();

    /// <summary>
    /// Gets the source from which this font face was read.
    /// </summary>
    public required Source Source { get; init; }

    /// <summary>
    /// Gets the table directory for this font face.
    /// </summary>
    public required SfntTableDirectory SfntDirectory { get; init; }

    /// <summary>
    /// Gets the directory of tables available in this font face.
    /// </summary>
    public override IReadOnlyDictionary<Tag, IFontTableEntry> Directory => SfntDirectory;

    /// <summary>
    /// Gets the SFNT version marker of this font face.
    /// </summary>
    public uint SfntVersion => SfntDirectory.SfntVersion;

    /// <inheritdoc/>
    public override T GetTable<T>()
    {
        if (!SfntDirectory.TryGetValue(T.Tag, out var record))
        {
            throw new InvalidDataException(
                $"Font does not contain a '{T.Tag}' ({typeof(T).Name}) table.");
        }

        return (T)_cache.GetOrAdd(T.Tag, Parse<T>, (this, record.Offset, record.Length));
    }

    private static IRecord Parse<T>(Tag tag, (SfntFontFace fontFace, uint Offset, uint Length) context) where T : IFontTable<T>
    {
        var cursor = context.fontFace.Source.CreateSliceCursor(context.Offset, context.Length);
        return T.Parse(ref cursor, context.fontFace);
    }

    /// <inheritdoc/>
    public override bool TryGetTable<T>(out T table)
    {
        if (!SfntDirectory.ContainsKey(T.Tag))
        {
            table = default!;
            return false;
        }

        table = GetTable<T>();
        return true;
    }

    /// <summary>
    /// Parses an SFNT font face from the supplied cursor.
    /// </summary>
    /// <param name="cursor">The cursor positioned at the SFNT header.</param>
    /// <param name="context">Additional parsing context, if any.</param>
    /// <returns>The parsed SFNT font face.</returns>
    static SfntFontFace IRecord<SfntFontFace>.Parse(ref Cursor cursor, object? context)
    {
        ArgumentNullException.ThrowIfNull(cursor.Source);
        return new()
        {
            Source = cursor.Source,
            SfntDirectory = cursor.ReadRecord<SfntTableDirectory>()
        };
    }
}
