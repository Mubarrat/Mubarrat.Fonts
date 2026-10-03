using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType.Tables.Variations;

/// <summary>The <c>HVAR</c> table: horizontal metric variations for variable fonts.</summary>
/// <remarks>The table contains an <see cref="ItemVariationStore"/> for advance-width and side-bearing deltas, with optional <see cref="DeltaSetIndexMap"/> tables for remapping advance-width, left-side-bearing, and right-side-bearing indices. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hvar"><c>HVAR</c> specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hvar"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon"/>
public sealed record HvarTable : IOpenTypeTable<HvarTable>
{
    /// <summary>Gets the OpenType table tag <c>HVAR</c>.</summary>
    public static Tag Tag => "HVAR";

    /// <summary>Gets the major version.</summary>
    /// <remarks>Must be 1.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hvar"/>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version.</summary>
    /// <remarks>Must be 0 for the current <c>HVAR</c> format.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hvar"/>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the item variation store containing the horizontal metric deltas.</summary>
    /// <remarks>The store contains variation deltas for advance widths and side bearings.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hvar"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-store"/>
    public ItemVariationStore Store { get; init; } = null!;

    /// <summary>Gets the advance-width delta-set index map, or <c>null</c> when implicit indexing applies.</summary>
    /// <remarks>When present, the map converts glyph or metric indices into explicit item variation store outer and inner indices.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hvar"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#delta-set-index-map"/>
    public DeltaSetIndexMap? AdvanceWidthMap { get; init; }

    /// <summary>Gets the left-side-bearing delta-set index map, or <c>null</c> when implicit indexing applies.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hvar"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#delta-set-index-map"/>
    public DeltaSetIndexMap? LeftSideBearingMap { get; init; }

    /// <summary>Gets the right-side-bearing delta-set index map, or <c>null</c> when implicit indexing applies.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hvar"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#delta-set-index-map"/>
    public DeltaSetIndexMap? RightSideBearingMap { get; init; }

    /// <summary>The 20-byte fixed-layout <c>HVAR</c> table header.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hvar"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>The major version at byte offset 0.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hvar"/>
        public ushort Major;

        /// <summary>The minor version at byte offset 2.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hvar"/>
        public ushort Minor;

        /// <summary>The offset to the <see cref="ItemVariationStore"/> at byte offset 4.</summary>
        /// <remarks>The offset is measured from the beginning of the <c>HVAR</c> table.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hvar"/>
        public uint ItemVariationStoreOffset;

        /// <summary>The offset to the advance-width mapping at byte offset 8.</summary>
        /// <remarks>Zero indicates that the optional mapping is absent.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hvar"/>
        public uint AdvanceWidthMappingOffset;

        /// <summary>The offset to the left-side-bearing mapping at byte offset 12.</summary>
        /// <remarks>Zero indicates that the optional mapping is absent.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hvar"/>
        public uint LsbMappingOffset;

        /// <summary>The offset to the right-side-bearing mapping at byte offset 16.</summary>
        /// <remarks>Zero indicates that the optional mapping is absent.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hvar"/>
        public uint RsbMappingOffset;

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        public static Header ReverseEndianness(Header value) => new()
        {
            Major = BinaryPrimitives.ReverseEndianness(value.Major),
            Minor = BinaryPrimitives.ReverseEndianness(value.Minor),
            ItemVariationStoreOffset = BinaryPrimitives.ReverseEndianness(value.ItemVariationStoreOffset),
            AdvanceWidthMappingOffset = BinaryPrimitives.ReverseEndianness(value.AdvanceWidthMappingOffset),
            LsbMappingOffset = BinaryPrimitives.ReverseEndianness(value.LsbMappingOffset),
            RsbMappingOffset = BinaryPrimitives.ReverseEndianness(value.RsbMappingOffset),
        };
    }

    /// <inheritdoc/>
    public static HvarTable Parse(ref Cursor cursor, object? context)
    {
        Header h = cursor.ReadBigEndianStruct<Header>();

        if (h.Major != 1)
            throw new InvalidDataException($"'HVAR'.majorVersion is {h.Major}, expected 1.");
        if (h.ItemVariationStoreOffset == 0)
            throw new InvalidDataException("'HVAR'.itemVariationStoreOffset is 0.");

        return new HvarTable
        {
            MajorVersion = h.Major,
            MinorVersion = h.Minor,
            Store = cursor.Source.ParseRecordAt<ItemVariationStore>(h.ItemVariationStoreOffset),
            AdvanceWidthMap = h.AdvanceWidthMappingOffset != 0
                ? cursor.Source.ParseRecordAt<DeltaSetIndexMap>(h.AdvanceWidthMappingOffset)
                : null,
            LeftSideBearingMap = h.LsbMappingOffset != 0
                ? cursor.Source.ParseRecordAt<DeltaSetIndexMap>(h.LsbMappingOffset)
                : null,
            RightSideBearingMap = h.RsbMappingOffset != 0
                ? cursor.Source.ParseRecordAt<DeltaSetIndexMap>(h.RsbMappingOffset)
                : null,
        };
    }
}
