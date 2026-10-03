using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType.Tables.Variations;

/// <summary>A fixed-layout MVAR value record identifying a font-wide metric and its item variation store delta-set index.</summary>
/// <remarks>The record contains a four-byte value tag followed by the outer and inner indices of the corresponding delta set. The on-disk size is 8 bytes. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-record">MVAR ValueRecord specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-record"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct MvarValueRecord : IBigEndianStruct<MvarValueRecord>
{
    /// <summary>Gets the four-byte tag identifying the varied font-wide metric.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public Tag ValueTag;

    /// <summary>Gets the outer index selecting an <see cref="ItemVariationData"/> subtable.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-record"/>
    public ushort DeltaSetOuterIndex;

    /// <summary>Gets the inner index selecting a delta-set row within the selected <see cref="ItemVariationData"/> subtable.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-record"/>
    public ushort DeltaSetInnerIndex;

    /// <summary>Reverses the byte order of every field in an <see cref="MvarValueRecord"/> value.</summary>
    public static MvarValueRecord ReverseEndianness(MvarValueRecord value) => new()
    {
        ValueTag = Tag.ReverseEndianness(value.ValueTag),
        DeltaSetOuterIndex = BinaryPrimitives.ReverseEndianness(value.DeltaSetOuterIndex),
        DeltaSetInnerIndex = BinaryPrimitives.ReverseEndianness(value.DeltaSetInnerIndex),
    };
}

/// <summary>The <c>MVAR</c> table: metrics variations for font-wide metric values.</summary>
/// <remarks>The table associates four-byte value tags with delta-set indices into an <see cref="ItemVariationStore"/>. The target values reside in other font tables such as <c>OS/2</c>, <c>hhea</c>, <c>vhea</c>, <c>gasp</c>, and <c>post</c>. Value records are stored in binary order by <see cref="MvarValueRecord.ValueTag"/>. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar">MVAR specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#item-variation-store"/>
public sealed record MvarTable : IOpenTypeTable<MvarTable>
{
    /// <summary>Gets the OpenType table tag <c>MVAR</c>.</summary>
    public static Tag Tag => "MVAR";

    /// <summary>Gets the major version.</summary>
    /// <remarks>Must be 1.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar"/>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version.</summary>
    /// <remarks>Must be 0 for the current <c>MVAR</c> format.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar"/>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the value records identifying the varied font-wide metrics and their delta-set indices.</summary>
    /// <remarks>The records are expected to be in binary order by <see cref="MvarValueRecord.ValueTag"/>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-record"/>
    public IReadOnlyList<MvarValueRecord> ValueRecords { get; init; } = [];

    /// <summary>Gets the item variation store containing the metric deltas.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#item-variation-store"/>
    public ItemVariationStore Store { get; init; } = null!;

    /// <summary>Finds the value record identified by <paramref name="tag"/>, or <c>null</c> when the metric is not varied.</summary>
    /// <param name="tag">The four-byte MVAR value tag to find.</param>
    /// <returns>The matching value record, or <c>null</c> when no record has the specified tag.</returns>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#processing"/>
    public MvarValueRecord? Find(Tag tag)
    {
        for (int i = 0; i < ValueRecords.Count; i++)
            if (ValueRecords[i].ValueTag == tag) return ValueRecords[i];
        return null;
    }

    /// <summary>Gets the size of an MVAR value record in bytes.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-record"/>
    public const int ValueRecordSize = 8;

    /// <summary>The 12-byte fixed-layout header of an <c>MVAR</c> table.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>The major version at byte offset 0.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar"/>
        public ushort Major;

        /// <summary>The minor version at byte offset 2.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar"/>
        public ushort Minor;

        /// <summary>The reserved field at byte offset 4.</summary>
        /// <remarks>Must be zero.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar"/>
        public ushort Reserved;

        /// <summary>The declared size of each value record at byte offset 6.</summary>
        /// <remarks>The current value record format occupies 8 bytes; future minor-version extensions may append fields.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar"/>
        public ushort ValueRecordSize;

        /// <summary>The number of value records at byte offset 8.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar"/>
        public ushort ValueRecordCount;

        /// <summary>The 16-bit offset to the item variation store at byte offset 10.</summary>
        /// <remarks>The offset is measured from the beginning of the <c>MVAR</c> table.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar"/>
        public ushort ItemVariationStoreOffset;

        /// <summary>Reverses the byte order of every field in an <see cref="Header"/> value.</summary>
        public static Header ReverseEndianness(Header value) => new()
        {
            Major = BinaryPrimitives.ReverseEndianness(value.Major),
            Minor = BinaryPrimitives.ReverseEndianness(value.Minor),
            Reserved = BinaryPrimitives.ReverseEndianness(value.Reserved),
            ValueRecordSize = BinaryPrimitives.ReverseEndianness(value.ValueRecordSize),
            ValueRecordCount = BinaryPrimitives.ReverseEndianness(value.ValueRecordCount),
            ItemVariationStoreOffset = BinaryPrimitives.ReverseEndianness(value.ItemVariationStoreOffset),
        };
    }

    /// <summary>Reads an <see cref="MvarTable"/> from <paramref name="cursor"/>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar"/>
    public static MvarTable Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Major != 1)
            throw new InvalidDataException($"'MVAR'.majorVersion is {header.Major}, expected 1.");
        if (header.ValueRecordSize < ValueRecordSize)
            throw new InvalidDataException(
                $"'MVAR'.valueRecordSize is {header.ValueRecordSize}, expected at least {ValueRecordSize}.");
        if (header.ItemVariationStoreOffset == 0)
            throw new InvalidDataException("'MVAR'.itemVariationStoreOffset is 0.");

        var records = ParseValueRecords(ref cursor, header.ValueRecordCount, header.ValueRecordSize);

        var store = cursor.Source.ParseRecordAt<ItemVariationStore>(header.ItemVariationStoreOffset);

        return new MvarTable
        {
            MajorVersion = header.Major,
            MinorVersion = header.Minor,
            ValueRecords = records,
            Store = store,
        };
    }

    /// <summary>Reads <paramref name="count"/> MVAR value records using the declared record stride.</summary>
    /// <param name="cursor">The cursor positioned at the first value record.</param>
    /// <param name="count">The number of value records to read.</param>
    /// <param name="declaredSize">The declared size of each value record in bytes.</param>
    /// <returns>The parsed value records.</returns>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar"/>
    public static MvarValueRecord[] ParseValueRecords(ref Cursor cursor, int count, ushort declaredSize)
    {
        if (count == 0) return [];

        if (declaredSize == ValueRecordSize)
        {
            // Fast path: declared size matches the struct, one batch read.
            return cursor.ReadBigEndianStructArray<MvarValueRecord>(count);
        }

        // Forward-compatible path: records are larger than the fields we understand;
        // step through at the declared stride.
        var records = new MvarValueRecord[count];
        for (int i = 0; i < count; i++)
        {
            records[i] = cursor.PeekBigEndianStruct<MvarValueRecord>();
            cursor.Position += declaredSize;
        }
        return records;
    }
}

/// <summary>The standard value tags defined for the <c>MVAR</c> table.</summary>
/// <remarks>Each tag identifies a specific font-wide metric or value in another OpenType table. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags">MVAR value-tag specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
public static class MvarValueTags
{
    /// <summary>Identifies <c>OS/2.sTypoAscender</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag Hascender = "hasc";

    /// <summary>Identifies <c>OS/2.sTypoDescender</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag Hdescender = "hdsc";

    /// <summary>Identifies <c>OS/2.sTypoLineGap</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag HlineGap = "hlgp";

    /// <summary>Identifies <c>hhea.caretSlopeRise</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag HclipAscender = "hcrs";

    /// <summary>Identifies <c>hhea.caretSlopeRun</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag HclipDescender = "hcrn";

    /// <summary>Identifies <c>hhea.caretOffset</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag HcaretOffset = "hcof";

    /// <summary>Identifies <c>vhea.ascent</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag Vascender = "vasc";

    /// <summary>Identifies <c>vhea.descent</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag Vdescender = "vdsc";

    /// <summary>Identifies <c>vhea.lineGap</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag VlineGap = "vlgp";

    /// <summary>Identifies <c>OS/2.sxHeight</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag XHeight = "xhgt";

    /// <summary>Identifies <c>OS/2.sCapHeight</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag CapHeight = "cpht";

    /// <summary>Identifies <c>OS/2.ySubscriptXSize</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag SubscriptXSize = "sbxs";

    /// <summary>Identifies <c>OS/2.ySubscriptYSize</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag SubscriptYSize = "sbys";

    /// <summary>Identifies <c>OS/2.ySubscriptXOffset</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag SubscriptXOffset = "sbxo";

    /// <summary>Identifies <c>OS/2.ySubscriptYOffset</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag SubscriptYOffset = "sbyo";

    /// <summary>Identifies <c>OS/2.ySuperscriptXSize</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag SuperscriptXSize = "spxs";

    /// <summary>Identifies <c>OS/2.ySuperscriptYSize</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag SuperscriptYSize = "spys";

    /// <summary>Identifies <c>OS/2.ySuperscriptXOffset</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag SuperscriptXOffset = "spxo";

    /// <summary>Identifies <c>OS/2.ySuperscriptYOffset</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag SuperscriptYOffset = "spyo";

    /// <summary>Identifies <c>OS/2.yStrikeoutSize</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag StrikeoutSize = "strs";

    /// <summary>Identifies <c>OS/2.yStrikeoutPosition</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag StrikeoutOffset = "stro";

    /// <summary>Identifies <c>post.underlineThickness</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag UnderlineSize = "unds";

    /// <summary>Identifies <c>post.underlinePosition</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar#value-tags"/>
    public static readonly Tag UnderlineOffset = "undo";
}
