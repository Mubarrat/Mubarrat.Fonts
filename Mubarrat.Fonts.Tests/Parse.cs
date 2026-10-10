using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Sfnt;

namespace Mubarrat.Fonts.Tests;

internal static class Parse
{
    public static T Table<T>(byte[] bytes, object? context = null) where T : IRecord<T> => new MemorySource(bytes).ParseRecordAt<T>(0, context);

    public static FontFace Face(byte[] bytes) => new MemorySource(bytes).ParseRecordAt<SfntFontFace>(0);
}
