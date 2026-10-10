using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>cvar</c> table: variations of control value table (CVT) entries.</summary>
/// <remarks>The table contains a single <see cref="TupleVariationStore"/> whose tuple variation headers carry their peak tuples directly. Unlike <c>gvar</c>, <c>cvar</c> has no shared tuple array. The store's <c>dataOffset</c> is measured from the beginning of the store, immediately after the 4-byte <c>cvar</c> version prefix. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cvar"><c>cvar</c> specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cvar"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon"/>
public sealed record CvarTable : IFontTable<CvarTable>
{
    /// <summary>Gets the OpenType table tag <c>cvar</c>.</summary>
    public static Tag Tag => "cvar";

    /// <summary>Gets the major version.</summary>
    /// <remarks>Must be 1.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cvar"/>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version.</summary>
    /// <remarks>Must be 0 for the current <c>cvar</c> format.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cvar"/>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the tuple variation store containing the CVT deltas.</summary>
    /// <remarks>The store contains the tuple variations that apply deltas to CVT entries across the font's variation space. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon">OpenType tuple variation store specification</see>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cvar"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon"/>
    public TupleVariationStore Store { get; init; } = null!;

    /// <inheritdoc/>
    public static CvarTable Parse(ref Cursor cursor, object? context)
    {
        var face = (FontFace)context!;
        int axisCount = face.GetTable<FvarTable>().AxisCount;

        ushort major = cursor.ReadUInt16();
        ushort minor = cursor.ReadUInt16();
        if (major != 1)
            throw new InvalidDataException($"'cvar'.majorVersion is {major}, expected 1.");

        var store = TupleVariationStore.Parse(ref cursor, axisCount, isCvar: true);

        return new CvarTable
        {
            MajorVersion = major,
            MinorVersion = minor,
            Store = store,
        };
    }
}
