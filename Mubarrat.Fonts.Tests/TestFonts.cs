namespace Mubarrat.Fonts.Tests;

internal static class TestFonts
{
    private static readonly string Dir =
        System.IO.Path.Combine(AppContext.BaseDirectory, "Fonts");

    /// <summary>All .ttf and .otf files in the Fonts folder, by path.</summary>
    public static IEnumerable<string> AllPaths =>
        Directory.EnumerateFiles(Dir, "*.*", SearchOption.TopDirectoryOnly)
                 .Where(p => p.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
                             p.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
                 .OrderBy(p => p, StringComparer.Ordinal);

    public static byte[] Bytes(string path) => File.ReadAllBytes(path);

    public static string Path(string fileName) =>
        System.IO.Path.Combine(Dir, fileName);
}
