using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.OpenType.Tables.Variations;

/// <summary>The <c>VVAR</c> table, which provides variation deltas for vertical metrics.</summary>
/// <remarks>The table stores deltas in an <see cref="ItemVariationStore"/> and optionally maps glyph metrics to variation-data indices for advance height, top side bearing, bottom side bearing, and vertical origin.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vvar">OpenType <c>VVAR</c> Table</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon">OpenType Variation Common Formats</seealso>
public sealed record VvarTable : IOpenTypeTable<VvarTable>
{
    /// <summary>Gets the OpenType table tag <c>VVAR</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vvar">OpenType <c>VVAR</c> Table</seealso>
    public static Tag Tag => "VVAR";

    /// <summary>Gets the major version of the <c>VVAR</c> table.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vvar">OpenType <c>VVAR</c> Table</seealso>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version of the <c>VVAR</c> table.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vvar">OpenType <c>VVAR</c> Table</seealso>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the item variation store containing the vertical metric deltas.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vvar">OpenType <c>VVAR</c> Table</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-store">OpenType Item Variation Store</seealso>
    public ItemVariationStore Store { get; init; } = null!;

    /// <summary>Gets the delta-set index map for advance heights, or <c>null</c> when no map is present.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vvar">OpenType <c>VVAR</c> Table</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#delta-set-index-map">OpenType Delta Set Index Map</seealso>
    public DeltaSetIndexMap? AdvanceHeightMap { get; init; }

    /// <summary>Gets the delta-set index map for top side bearings, or <c>null</c> when no map is present.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vvar">OpenType <c>VVAR</c> Table</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#delta-set-index-map">OpenType Delta Set Index Map</seealso>
    public DeltaSetIndexMap? TopSideBearingMap { get; init; }

    /// <summary>Gets the delta-set index map for bottom side bearings, or <c>null</c> when no map is present.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vvar">OpenType <c>VVAR</c> Table</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#delta-set-index-map">OpenType Delta Set Index Map</seealso>
    public DeltaSetIndexMap? BottomSideBearingMap { get; init; }

    /// <summary>Gets the delta-set index map for vertical origins, or <c>null</c> when no map is present.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vvar">OpenType <c>VVAR</c> Table</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#delta-set-index-map">OpenType Delta Set Index Map</seealso>
    public DeltaSetIndexMap? VerticalOriginMap { get; init; }

    /// <summary>The fixed-layout 28-byte <c>VVAR</c> table header.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vvar">OpenType <c>VVAR</c> Table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Major version at offset +0.</summary>
        public ushort Major;

        /// <summary>Minor version at offset +2.</summary>
        public ushort Minor;

        /// <summary>Offset to the item variation store from the beginning of the <c>VVAR</c> table, at offset +4.</summary>
        public uint StoreOffset;

        /// <summary>Offset to the advance height delta-set index map from the beginning of the <c>VVAR</c> table, at offset +8; zero when absent.</summary>
        public uint AdvanceHeightMapOffset;

        /// <summary>Offset to the top side bearing delta-set index map from the beginning of the <c>VVAR</c> table, at offset +12; zero when absent.</summary>
        public uint TopSideBearingMapOffset;

        /// <summary>Offset to the bottom side bearing delta-set index map from the beginning of the <c>VVAR</c> table, at offset +16; zero when absent.</summary>
        public uint BottomSideBearingMapOffset;

        /// <summary>Offset to the vertical origin delta-set index map from the beginning of the <c>VVAR</c> table, at offset +20; zero when absent.</summary>
        public uint VerticalOriginMapOffset;

        /// <summary>Reverses the byte order of all fields in the header.</summary>
        public static Header ReverseEndianness(Header value) => new()
        {
            Major = BinaryPrimitives.ReverseEndianness(value.Major),
            Minor = BinaryPrimitives.ReverseEndianness(value.Minor),
            StoreOffset = BinaryPrimitives.ReverseEndianness(value.StoreOffset),
            AdvanceHeightMapOffset = BinaryPrimitives.ReverseEndianness(value.AdvanceHeightMapOffset),
            TopSideBearingMapOffset = BinaryPrimitives.ReverseEndianness(value.TopSideBearingMapOffset),
            BottomSideBearingMapOffset = BinaryPrimitives.ReverseEndianness(value.BottomSideBearingMapOffset),
            VerticalOriginMapOffset = BinaryPrimitives.ReverseEndianness(value.VerticalOriginMapOffset),
        };
    }

    /// <summary>Parses a <c>VVAR</c> table from <paramref name="cursor"/>.</summary>
    /// <param name="cursor">Cursor positioned at the beginning of the <c>VVAR</c> table.</param>
    /// <param name="context">Parsing context passed to referenced records.</param>
    /// <returns>The parsed <see cref="VvarTable"/>.</returns>
    /// <exception cref="InvalidDataException">The table uses an unsupported major version.</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vvar">OpenType <c>VVAR</c> Table</seealso>
    static VvarTable IRecord<VvarTable>.Parse(ref Cursor cursor, object? context)
    {
        Header h = cursor.ReadBigEndianStruct<Header>();

        if (h.Major != 1)
            throw new InvalidDataException($"'VVAR'.majorVersion is {h.Major}, expected 1.");

        return new VvarTable
        {
            MajorVersion = h.Major,
            MinorVersion = h.Minor,
            Store = cursor.Source.ParseRecordAt<ItemVariationStore>(h.StoreOffset, context),
            AdvanceHeightMap = h.AdvanceHeightMapOffset != 0 ? cursor.Source.ParseRecordAt<DeltaSetIndexMap>(h.AdvanceHeightMapOffset, context) : null,
            TopSideBearingMap = h.TopSideBearingMapOffset != 0 ? cursor.Source.ParseRecordAt<DeltaSetIndexMap>(h.TopSideBearingMapOffset, context) : null,
            BottomSideBearingMap = h.BottomSideBearingMapOffset != 0 ? cursor.Source.ParseRecordAt<DeltaSetIndexMap>(h.BottomSideBearingMapOffset, context) : null,
            VerticalOriginMap = h.VerticalOriginMapOffset != 0 ? cursor.Source.ParseRecordAt<DeltaSetIndexMap>(h.VerticalOriginMapOffset, context) : null,
        };
    }
}
