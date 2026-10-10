using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>fvar</c> table: global definition of the variation axes and named instances of a variable font.</summary>
/// <remarks>The table defines the user-space coordinate system for variation axes. Other variation tables use normalized coordinates in the range [-1, 1]; <see cref="VariationAxisRecord.Normalize"/> performs the per-axis conversion. The <c>fvar</c> axis order is also used by variation data such as <c>gvar</c>, <c>avar</c>, <c>cvar</c>, <c>HVAR</c>, <c>VVAR</c>, and <c>MVAR</c>. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"><c>fvar</c> specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
public sealed record FvarTable : IFontTable<FvarTable>
{
    /// <summary>Gets the OpenType table tag <c>fvar</c>.</summary>
    public static Tag Tag => "fvar";

    /// <summary>Gets the major version.</summary>
    /// <remarks>Must be 1.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version.</summary>
    /// <remarks>Must be 0 for the current <c>fvar</c> format.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the variation axes in table order.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#variationaxisrecord"/>
    public IReadOnlyList<VariationAxisRecord> Axes { get; init; } = [];

    /// <summary>Gets the named instances in table order.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#namedinstance"/>
    public IReadOnlyList<NamedInstance> Instances { get; init; } = [];

    /// <summary>Gets the number of variation axes.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
    public int AxisCount => Axes.Count;

    /// <summary>Gets the number of named instances.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
    public int InstanceCount => Instances.Count;

    /// <summary>Gets a value indicating whether the font has no variation axes.</summary>
    /// <remarks>When zero axes are present, the specification directs applications to treat the font as non-variable and ignore variation-specific tables.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
    public bool IsNonVariable => AxisCount == 0;

    /// <summary>Gets the declared size of each variation axis record, in bytes.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
    public ushort DeclaredAxisSize { get; init; }

    /// <summary>Gets the declared size of each named instance record, in bytes.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
    public ushort DeclaredInstanceSize { get; init; }

    /// <summary>Gets the variation axis identified by <paramref name="tag"/>, or <c>null</c> when the axis is not defined.</summary>
    /// <param name="tag">The four-byte variation axis tag.</param>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#variationaxisrecord"/>
    public VariationAxisRecord? GetAxis(Tag tag)
    {
        for (int i = 0; i < Axes.Count; i++)
            if (Axes[i].Tag == tag) return Axes[i];
        return null;
    }

    /// <summary>Gets the zero-based index of the variation axis identified by <paramref name="tag"/>, or <c>-1</c> when the axis is not defined.</summary>
    /// <param name="tag">The four-byte variation axis tag.</param>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#variationaxisrecord"/>
    public int GetAxisIndex(Tag tag)
    {
        for (int i = 0; i < Axes.Count; i++)
            if (Axes[i].Tag == tag) return i;
        return -1;
    }

    /// <summary>Normalizes user-space <paramref name="coordinates"/> to the F2Dot14 normalized coordinate range used by variation tables.</summary>
    /// <param name="coordinates">One user-space coordinate for each axis, in <see cref="Axes"/> order.</param>
    /// <returns>The normalized coordinates as <see cref="F2Dot14"/> values.</returns>
    /// <exception cref="ArgumentException"><paramref name="coordinates"/> does not contain exactly <see cref="AxisCount"/> values.</exception>
    /// <remarks>Each coordinate is normalized independently using its corresponding <see cref="VariationAxisRecord"/>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#variationaxisrecord"/>
    public F2Dot14[] Normalize(ReadOnlySpan<double> coordinates)
    {
        if (coordinates.Length != AxisCount)
            throw new ArgumentException(
                $"Expected {AxisCount} coordinates, got {coordinates.Length}.",
                nameof(coordinates));

        var result = new F2Dot14[AxisCount];
        for (int i = 0; i < AxisCount; i++)
            result[i] = F2Dot14.FromDouble(Axes[i].Normalize(coordinates[i]));
        return result;
    }

    /// <summary>Normalizes user-space <paramref name="coordinates"/> and returns the results as <see cref="double"/> values.</summary>
    /// <param name="coordinates">One user-space coordinate for each axis, in <see cref="Axes"/> order.</param>
    /// <returns>The normalized coordinates as <see cref="double"/> values.</returns>
    /// <exception cref="ArgumentException"><paramref name="coordinates"/> does not contain exactly <see cref="AxisCount"/> values.</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#variationaxisrecord"/>
    public double[] NormalizeToDouble(ReadOnlySpan<double> coordinates)
    {
        var normalized = Normalize(coordinates);
        var result = new double[normalized.Length];
        for (int i = 0; i < normalized.Length; i++)
            result[i] = normalized[i].Value;
        return result;
    }

    /// <summary>The 16-byte fixed-layout <c>fvar</c> table header.</summary>
    /// <remarks>Fields are stored in big-endian order at the offsets defined by the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"><c>fvar</c> specification</see>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The major version at byte offset 0.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
        public ushort MajorVersion;

        /// <summary>The minor version at byte offset 2.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
        public ushort MinorVersion;

        /// <summary>The offset to the variation axis array at byte offset 4.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
        public ushort AxesArrayOffset;

        /// <summary>The reserved field at byte offset 6.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
        public ushort Reserved;

        /// <summary>The number of variation axes at byte offset 8.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
        public ushort AxisCount;

        /// <summary>The size of each variation axis record at byte offset 10.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
        public ushort AxisSize;

        /// <summary>The number of named instances at byte offset 12.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
        public ushort InstanceCount;

        /// <summary>The size of each named instance record at byte offset 14.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
        public ushort InstanceSize;

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        public static Header ReverseEndianness(Header value) => new()
        {
            MajorVersion = BinaryPrimitives.ReverseEndianness(value.MajorVersion),
            MinorVersion = BinaryPrimitives.ReverseEndianness(value.MinorVersion),
            AxesArrayOffset = BinaryPrimitives.ReverseEndianness(value.AxesArrayOffset),
            Reserved = BinaryPrimitives.ReverseEndianness(value.Reserved),
            AxisCount = BinaryPrimitives.ReverseEndianness(value.AxisCount),
            AxisSize = BinaryPrimitives.ReverseEndianness(value.AxisSize),
            InstanceCount = BinaryPrimitives.ReverseEndianness(value.InstanceCount),
            InstanceSize = BinaryPrimitives.ReverseEndianness(value.InstanceSize),
        };
    }

    /// <summary>The minimum size of a variation axis record on disk, in bytes.</summary>
    /// <remarks>The current <c>VariationAxisRecord</c> layout occupies 20 bytes.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#variationaxisrecord"/>
    public const int AxisRecordSize = 20;

    /// <inheritdoc/>
    public static FvarTable Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.MajorVersion != 1)
            throw new InvalidDataException(
                $"'fvar'.majorVersion is {header.MajorVersion}, expected 1.");

        if (header.AxisSize < AxisRecordSize)
            throw new InvalidDataException(
                $"'fvar'.axisSize is {header.AxisSize}, expected at least {AxisRecordSize}.");

        // Instance records are axisCount Fixed coordinates plus a 4-byte prefix and an
        // optional 2-byte postScriptNameID suffix.
        int minInstanceSize = header.AxisCount * 4 + 4;
        int fullInstanceSize = header.AxisCount * 4 + 6;
        if (header.InstanceSize != minInstanceSize && header.InstanceSize != fullInstanceSize)
            throw new InvalidDataException(
                $"'fvar'.instanceSize is {header.InstanceSize}, expected {minInstanceSize} or {fullInstanceSize}.");

        bool hasPostScriptNameID = header.InstanceSize == fullInstanceSize;

        // Axes array is at AxesArrayOffset within the fvar table. Since the cursor's
        // source is anchored at the fvar table start and the header read advanced the
        // cursor by 16 bytes, seek to the declared offset.
        cursor.Position = header.AxesArrayOffset;

        var axes = new VariationAxisRecord[header.AxisCount];
        if (header.AxisSize == AxisRecordSize)
        {
            // Fast path: records are exactly the size we expect, so we can read them all at once.
            cursor.ReadBigEndianStructArray(axes);
        }
        else
        {
            for (int i = 0; i < axes.Length; i++)
            {
                // Forward-compatible path: records are larger than the fields we understand.
                axes[i] = cursor.PeekBigEndianStruct<VariationAxisRecord>();
                cursor.Position += header.AxisSize;
            }
        }

        var instances = new NamedInstance[header.InstanceCount];
        for (int i = 0; i < instances.Length; i++)
        {
            long recordStart = cursor.Position;

            ushort subfamilyNameID = cursor.ReadUInt16();
            ushort flags = cursor.ReadUInt16();

            var coordinates = new double[header.AxisCount];
            for (int a = 0; a < header.AxisCount; a++)
                coordinates[a] = cursor.ReadFixed().Value;

            ushort? postScriptNameID = null;
            if (hasPostScriptNameID)
                postScriptNameID = cursor.ReadUInt16();

            instances[i] = new NamedInstance
            {
                SubfamilyNameID = subfamilyNameID,
                Flags = flags,
                Coordinates = coordinates,
                PostScriptNameID = postScriptNameID,
            };

            cursor.Position = recordStart + header.InstanceSize;
        }

        return new FvarTable
        {
            MajorVersion = header.MajorVersion,
            MinorVersion = header.MinorVersion,
            Axes = axes,
            Instances = instances,
            DeclaredAxisSize = header.AxisSize,
            DeclaredInstanceSize = header.InstanceSize,
        };
    }
}

/// <summary>A named instance: a predefined position in the font's variation space with associated name IDs.</summary>
/// <remarks>The coordinates are stored in user space and correspond to the axes in <see cref="FvarTable.Axes"/> order. The instance can optionally identify a PostScript name through the <c>name</c> table.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#namedinstance"/>
public sealed record NamedInstance
{
    /// <summary>Gets the <c>name</c> table ID for the instance's subfamily name.</summary>
    /// <remarks>For the default instance, the value is 2 or 17. For other instances, the value is greater than 255 and less than 32768.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#namedinstance"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name"/>
    public ushort SubfamilyNameID { get; init; }

    /// <summary>Gets the reserved flags field.</summary>
    /// <remarks>Must be 0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#namedinstance"/>
    public ushort Flags { get; init; }

    /// <summary>Gets the user-space axis coordinates of this instance in <see cref="FvarTable.Axes"/> order.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#namedinstance"/>
    public IReadOnlyList<double> Coordinates { get; init; } = [];

    /// <summary>Gets the <c>name</c> table ID for the instance's PostScript name, or <c>null</c> when no equivalent name is provided.</summary>
    /// <remarks>A raw value of <c>0xFFFF</c> represents no PostScript name and is exposed as <c>null</c>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#namedinstance"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name"/>
    public ushort? PostScriptNameID { get; init; }

    /// <summary>Gets a value indicating whether this instance provides a PostScript name.</summary>
    /// <remarks>Returns <c>false</c> when <see cref="PostScriptNameID"/> is <c>null</c> or equals <c>0xFFFF</c>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#namedinstance"/>
    public bool HasPostScriptName => PostScriptNameID is not null and not 0xFFFF;
}
