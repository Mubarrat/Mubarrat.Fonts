using Mubarrat.Fonts.OpenType.Binary;

namespace Mubarrat.Fonts.OpenType.Tables.Variations;

/// <summary>The <c>DeltaSetIndexMap</c> structure: maps implicit item positions to explicit outer and inner indices into an <see cref="ItemVariationStore"/>.</summary>
/// <remarks>The map supports format 0 with a 16-bit entry count and format 1 with a 32-bit entry count. Each entry is packed into 1–4 bytes; the <c>entryFormat</c> byte determines the entry width and the number of low-order bits assigned to the inner index. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#delta-set-index-map"><c>DeltaSetIndexMap</c> specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#delta-set-index-map"/>
public sealed record DeltaSetIndexMap : IRecord<DeltaSetIndexMap>
{
    /// <summary>Gets the map format: 0 or 1.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#delta-set-index-map"/>
    public ushort Format { get; init; }

    /// <summary>Gets the raw <c>entryFormat</c> byte.</summary>
    /// <remarks>Bits 4–5 encode the entry size minus one; bits 0–3 encode the inner-index bit count minus one.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#delta-set-index-map"/>
    public byte EntryFormat { get; init; }

    /// <summary>Gets the size of each packed map entry, in bytes.</summary>
    /// <remarks>The value is in the range 1–4.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#delta-set-index-map"/>
    public int EntrySize { get; init; }

    /// <summary>Gets the number of low-order bits assigned to the inner index of each entry.</summary>
    /// <remarks>The value is in the range 1–16.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#delta-set-index-map"/>
    public int InnerIndexBitCount { get; init; }

    /// <summary>Gets the number of map entries.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#delta-set-index-map"/>
    public int MapCount => Entries.Count;

    /// <summary>Gets the decoded outer and inner indices for each map entry.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#delta-set-index-map"/>
    public required IReadOnlyList<(int OuterIndex, int InnerIndex)> Entries { get; init; }

    /// <summary>Gets the outer and inner indices mapped from <paramref name="itemIndex"/>.</summary>
    /// <remarks>When <paramref name="itemIndex"/> is outside the map, returns <c>(0, 0)</c> as specified for an out-of-range item index.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#delta-set-index-map"/>
    public (int OuterIndex, int InnerIndex) GetIndex(int itemIndex)
    {
        if ((uint)itemIndex >= (uint)MapCount) return (0, 0);
        return Entries[itemIndex];
    }

    /// <summary>Reads a <see cref="DeltaSetIndexMap"/> from <paramref name="cursor"/>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#delta-set-index-map"/>
    public static DeltaSetIndexMap Parse(ref Cursor cursor, object? context)
    {
        byte format = cursor.ReadUInt8();
        byte entryFormat = cursor.ReadUInt8();

        int entrySize = ((entryFormat & 0x30) >> 4) + 1;
        int innerBitCount = (entryFormat & 0x0F) + 1;

        if (entrySize is < 1 or > 4)
            throw new InvalidDataException(
                $"DeltaSetIndexMap entrySize is {entrySize}, expected 1–4.");
        if (innerBitCount is < 1 or > 16)
            throw new InvalidDataException(
                $"DeltaSetIndexMap innerIndexBitCount is {innerBitCount}, expected 1–16.");

        int mapCount;
        switch (format)
        {
            case 0:
                mapCount = cursor.ReadUInt16();
                break;
            case 1:
                {
                    uint count = cursor.ReadUInt32();
                    if (count > int.MaxValue)
                        throw new InvalidDataException(
                            $"DeltaSetIndexMap mapCount {count} is too large.");
                    mapCount = (int)count;
                    break;
                }
            default:
                throw new InvalidDataException($"DeltaSetIndexMap format is {format}, expected 0 or 1.");
        }

        var entries = new (int, int)[mapCount];
        for (int i = 0; i < mapCount; i++)
        {
            uint packed = ReadEntry(ref cursor, entrySize);
            int innerMask = (1 << innerBitCount) - 1;
            int inner = (int)(packed & (uint)innerMask);
            int outer = (int)(packed >> innerBitCount);
            entries[i] = (outer, inner);
        }

        return new()
        {
            Format = format,
            EntryFormat = entryFormat,
            EntrySize = entrySize,
            InnerIndexBitCount = innerBitCount,
            Entries = entries
        };
    }

    /// <summary>Reads one big-endian packed map entry of the specified byte width.</summary>
    /// <param name="cursor">The cursor positioned at the entry.</param>
    /// <param name="entrySize">The entry width in bytes, from 1 through 4.</param>
    /// <returns>The packed unsigned entry value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="entrySize"/> is outside the range 1–4.</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#delta-set-index-map"/>
    public static uint ReadEntry(ref Cursor cursor, int entrySize)
    {
        if (entrySize is < 1 or > 4)
            throw new ArgumentOutOfRangeException(nameof(entrySize));

        uint value = 0;
        for (int i = 0; i < entrySize; i++)
            value = (value << 8) | cursor.ReadUInt8();
        return value;
    }
}
