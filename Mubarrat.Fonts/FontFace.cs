using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts;

/// <summary>
/// Represents a single font face, independently of its underlying storage
/// or font-file representation.
/// </summary>
/// <remarks>
/// <para>
/// A font face represents one face within a font resource. Its underlying
/// representation may be an SFNT font, a WOFF font, a WOFF2 font, or another
/// supported font format.
/// </para>
/// <para>
/// Use <see cref="FromFile(string)"/> to open a font face from a file and
/// select the appropriate representation-specific implementation.
/// </para>
/// <para>
/// This type defines the common abstraction. Format-specific implementations
/// expose the structures and operations associated with their respective
/// representations.
/// </para>
/// </remarks>
/// <seealso cref="Sfnt.SfntFontFace"/>
public abstract class FontFace
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FontFace"/> class.
    /// </summary>
    protected FontFace()
    {
    }

    /// <summary>
    /// Gets the directory of tables available in this font face.
    /// </summary>
    public abstract IReadOnlyDictionary<Tag, IFontTableEntry> Directory { get; }

    /// <summary>
    /// Gets the table of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The table type to retrieve.</typeparam>
    /// <returns>The parsed table.</returns>
    /// <exception cref="InvalidDataException">
    /// The font does not contain the requested table.
    /// </exception>
    public abstract T GetTable<T>() where T : IFontTable<T>;

    /// <summary>
    /// Attempts to get the table of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The table type to retrieve.</typeparam>
    /// <param name="table">
    /// When this method returns <see langword="true"/>, contains the table;
    /// otherwise, its default value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the table exists; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public abstract bool TryGetTable<T>(out T table) where T : IFontTable<T>;

    /// <summary>
    /// Opens a font face from a file.
    /// </summary>
    /// <param name="path">The path to the font file.</param>
    /// <returns>
    /// A font face represented by the appropriate implementation for the
    /// detected font format.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="path"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="path"/> is empty or consists only of whitespace.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// The file does not contain a supported single-face font representation.
    /// </exception>
    public static FontFace FromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // TODO: Detect SFNT, WOFF, and WOFF2 representations.
        throw new NotImplementedException();
    }
}
