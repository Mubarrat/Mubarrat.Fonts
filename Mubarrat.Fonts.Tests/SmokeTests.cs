using Mubarrat.Fonts.OpenType.Tables;

namespace Mubarrat.Fonts.Tests;

public class SmokeTests
{
    public static TheoryData<string> AllFonts
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var path in TestFonts.AllPaths)
                data.Add(path);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(AllFonts))]
    public void FontParses(string path)
    {
        var face = Parse.Face(TestFonts.Bytes(path));
        Assert.True(face.Directory.Count > 0);
    }

    [Theory]
    [MemberData(nameof(AllFonts))]
    public void CmapResolvesAscii(string path)
    {
        var face = Parse.Face(TestFonts.Bytes(path));
        var cmap = face.GetTable<CmapTable>();
        Assert.NotEqual(0, cmap.GetGlyphId('A'));
    }
}
