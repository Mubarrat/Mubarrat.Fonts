using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;
using System.Buffers.Binary;

namespace Mubarrat.Fonts.OpenType;

/// <summary>An entry in the sfnt table directory.</summary>
public record struct TableRecord : IBigEndianStruct<TableRecord>
{
    /// <summary>Gets the 4-byte table tag. This is the key used to look up the table in the directory.</summary>
    public Tag Tag;

    /// <summary>Gets the table's checksum as stored in the directory. This is not necessarily the same as the checksum of the table's bytes.</summary>
    public uint Checksum;

    /// <summary>Gets the absolute offset of the table from the start of the font stream. This is where the table's bytes can be read.</summary>
    public uint Offset;

    /// <summary>Gets the length of the table in bytes. This is the number of bytes to read from <see cref="Offset"/> to get the table's data.</summary>
    public uint Length;

    /// <inheritdoc/>
    public static TableRecord ReverseEndianness(TableRecord value) => new()
    {
        Tag = Tag.ReverseEndianness(value.Tag),
        Checksum = BinaryPrimitives.ReverseEndianness(value.Checksum),
        Offset = BinaryPrimitives.ReverseEndianness(value.Offset),
        Length = BinaryPrimitives.ReverseEndianness(value.Length)
    };
}
