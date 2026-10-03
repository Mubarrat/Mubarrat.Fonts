using System.Collections.Concurrent;
using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType;

/// <summary>
/// A single font resource: one source and one table directory. The root of the record chain.
/// Tables are parsed on first request and cached for the lifetime of the face.
/// </summary>
public sealed record FontFace : IRecord<FontFace>
{
    private readonly ConcurrentDictionary<Tag, IRecord> _cache = new();

    /// <summary>
    /// Gets the source from which this font face was read.
    /// </summary>
    public required Source Source { get; init; }
    /// <summary>
    /// Gets the table directory for this font face.
    /// </summary>
    public required TableDirectory Directory { get; init; }
    /// <summary>
    /// Gets the sfnt version marker of this font face.
    /// </summary>
    public uint SfntVersion => Directory.SfntVersion;

    /// <summary>
    /// Parses the table of type <typeparamref name="T"/> from this face's directory, caching
    /// the result. Subsequent calls with the same tag return the same instance.
    /// </summary>
    /// <exception cref="InvalidDataException">The font does not contain the table.</exception>
    public T GetTable<T>() where T : IOpenTypeTable<T>
    {
        if (!Directory.TryGet(T.Tag, out var record))
            throw new InvalidDataException(
                $"Font does not contain a '{T.Tag}' ({typeof(T).Name}) table.");

        return (T)_cache.GetOrAdd(T.Tag, Parse<T>, (record.Offset, record.Length));
    }

    private IRecord Parse<T>(Tag tag, (uint Offset, uint Length) context) where T : IOpenTypeTable<T>
    {
        var cursor = Source.CreateSliceCursor(context.Offset, context.Length);
        return T.Parse(ref cursor, this);
    }

    /// <summary>
    /// Attempts to parse the table of type <typeparamref name="T"/>. Returns <c>false</c>
    /// without caching if the table is not present in the directory.
    /// </summary>
    public bool TryGetTable<T>(out T table) where T : IOpenTypeTable<T>
    {
        if (!Directory.Contains(T.Tag)) { table = default!; return false; }
        table = GetTable<T>();
        return true;
    }

    /// <summary>
    /// Gets the byte length recorded for <paramref name="tag"/> in the font directory.
    /// </summary>
    /// <exception cref="InvalidDataException">The tag is not present in the directory.</exception>
    public int GetTableLength(Tag tag)
    {
        if (!Directory.TryGet(tag, out var record))
            throw new InvalidDataException($"Font does not contain a '{tag}' table.");
        if (record.Length > int.MaxValue)
            throw new InvalidDataException($"'{tag}' table length {record.Length} is too large.");
        return (int)record.Length;
    }

    /// <summary>Opens the font face from a source containing an sfnt header at offset 0.</summary>
    static FontFace IRecord<FontFace>.Parse(ref Cursor cursor, object? context)
    {
        ArgumentNullException.ThrowIfNull(cursor.Source);
        return new() { Source = cursor.Source, Directory = cursor.ReadRecord<TableDirectory>() };
    }
}
