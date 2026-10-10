using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>maxp</c> table: the maximum profile that establishes the memory requirements of the font.</summary>
/// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp">OpenType maxp — Maximum Profile</see> defines version 0.5 for CFF outlines, containing only <c>numGlyphs</c>, and version 1.0 for TrueType outlines, containing the complete maximum-profile data. <see cref="NumGlyphs"/> is the authoritative glyph count used by glyph-indexed tables such as <c>loca</c>, <c>glyf</c>, and <c>hmtx</c>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp"/>
public sealed record MaxpTable : IFontTable<MaxpTable>
{
    /// <inheritdoc/>
    public static Tag Tag => "maxp";

    /// <summary>Gets the version of the table.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00005000">Version 0.5</see> is used with CFF outlines, while <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">version 1.0</see> is used with TrueType outlines.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp"/>
    public MaxpVersion Version { get; init; }

    /// <summary>Gets the number of glyphs in the font.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00005000">numGlyphs</see> is present in both supported <c>maxp</c> versions and is the authoritative glyph count for the font.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00005000"/>
    public ushort NumGlyphs { get; init; }

    /// <summary>Gets the maximum number of points in a non-composite glyph.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxPoints</see> is present only in version 1.0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
    public ushort? MaxPoints { get; init; }

    /// <summary>Gets the maximum number of contours in a non-composite glyph.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxContours</see> is present only in version 1.0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
    public ushort? MaxContours { get; init; }

    /// <summary>Gets the maximum number of points in a composite glyph.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxCompositePoints</see> is present only in version 1.0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
    public ushort? MaxCompositePoints { get; init; }

    /// <summary>Gets the maximum number of contours in a composite glyph.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxCompositeContours</see> is present only in version 1.0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
    public ushort? MaxCompositeContours { get; init; }

    /// <summary>Gets the maximum number of zones used by TrueType instructions.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxZones</see> is 1 when instructions do not use the twilight zone and 2 when they do; it is present only in version 1.0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
    public ushort? MaxZones { get; init; }

    /// <summary>Gets the maximum number of points used in the twilight zone.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxTwilightPoints</see> is present only in version 1.0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
    public ushort? MaxTwilightPoints { get; init; }

    /// <summary>Gets the number of Storage Area locations.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxStorage</see> is present only in version 1.0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
    public ushort? MaxStorage { get; init; }

    /// <summary>Gets the number of function definitions.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxFunctionDefs</see> is equal to the highest function number plus one and is present only in version 1.0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
    public ushort? MaxFunctionDefs { get; init; }

    /// <summary>Gets the number of instruction definitions.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxInstructionDefs</see> is present only in version 1.0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
    public ushort? MaxInstructionDefs { get; init; }

    /// <summary>Gets the maximum stack depth required by TrueType programs and glyph instructions.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxStackElements</see> is the maximum stack depth across the Font Program, CVT Program, and glyph instructions.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
    public ushort? MaxStackElements { get; init; }

    /// <summary>Gets the maximum byte count of glyph instructions.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxSizeOfInstructions</see> is present only in version 1.0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
    public ushort? MaxSizeOfInstructions { get; init; }

    /// <summary>Gets the maximum number of component elements referenced at the top level of a composite glyph.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxComponentElements</see> is present only in version 1.0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
    public ushort? MaxComponentElements { get; init; }

    /// <summary>Gets the maximum component recursion depth.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxComponentDepth</see> is 1 for simple components and is present only in version 1.0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
    public ushort? MaxComponentDepth { get; init; }

    /// <summary>Gets a value indicating whether the table is version 1.0 and its TrueType-specific fields are populated.</summary>
    /// <remarks>This is true when <see cref="Version"/> is <see cref="MaxpVersion.Version10"/> and false for the CFF-oriented version 0.5 profile.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
    public bool HasTrueTypeFields => Version == MaxpVersion.Version10;

    /// <summary>Gets a value indicating whether the table is version 0.5 and contains only the glyph count.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00005000">Version 0.5</see> is used for fonts with CFF outlines.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00005000"/>
    public bool IsCffProfile => Version == MaxpVersion.Version05;

    /// <summary>Parses a <c>maxp</c> table from the current cursor position.</summary>
    /// <param name="cursor">The cursor positioned at the beginning of the table.</param>
    /// <param name="context">The parsing context; unused by this table.</param>
    /// <returns>The parsed <see cref="MaxpTable"/>.</returns>
    /// <exception cref="InvalidDataException">The table version is neither 0.5 nor 1.0.</exception>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp">The maxp table</see> begins with a 32-bit version field that determines whether the remaining layout is the 0.5 or 1.0 profile.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp"/>
    public static MaxpTable Parse(ref Cursor cursor, object? context)
    {
        // maxp is a leaf: parent is unused. Read the 4-byte version, then dispatch on it.
        // Version 0.5 is a single trailing ushort; version 1.0 has 14 more ushorts after
        // numGlyphs, read in one shot as a struct.
        uint rawVersion = cursor.ReadUInt32();

        if (rawVersion == (uint)MaxpVersion.Version05)
        {
            return new MaxpTable
            {
                Version = MaxpVersion.Version05,
                NumGlyphs = cursor.ReadUInt16(),
            };
        }

        if (rawVersion != (uint)MaxpVersion.Version10)
            throw new InvalidDataException(
                $"'maxp'.version is 0x{rawVersion:X8}, expected 0x00005000 or 0x00010000.");

        // Reads numGlyphs plus the 14 remaining ushorts in one shot. The struct starts at
        // numGlyphs because the version has already been consumed.
        Header10 h = cursor.ReadBigEndianStruct<Header10>();
        return new MaxpTable
        {
            Version = MaxpVersion.Version10,
            NumGlyphs = h.NumGlyphs,
            MaxPoints = h.MaxPoints,
            MaxContours = h.MaxContours,
            MaxCompositePoints = h.MaxCompositePoints,
            MaxCompositeContours = h.MaxCompositeContours,
            MaxZones = h.MaxZones,
            MaxTwilightPoints = h.MaxTwilightPoints,
            MaxStorage = h.MaxStorage,
            MaxFunctionDefs = h.MaxFunctionDefs,
            MaxInstructionDefs = h.MaxInstructionDefs,
            MaxStackElements = h.MaxStackElements,
            MaxSizeOfInstructions = h.MaxSizeOfInstructions,
            MaxComponentElements = h.MaxComponentElements,
            MaxComponentDepth = h.MaxComponentDepth,
        };
    }

    /// <summary>The on-disk layout of the version 1.0 <c>maxp</c> header following the version field.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">Version 1.0</see> stores <c>numGlyphs</c> followed by fourteen additional unsigned 16-bit fields. The version field itself is consumed separately by <see cref="Parse"/>, so this packed structure begins at <c>numGlyphs</c> and has an exact size of 28 bytes.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header10 : IEndianReversibleStruct<Header10>
    {
        /// <summary>Gets or sets the number of glyphs.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">numGlyphs</see> occupies offset 0 in the packed version 1.0 payload.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
        public ushort NumGlyphs;              // +0

        /// <summary>Gets or sets the maximum points in a non-composite glyph.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxPoints</see> occupies offset 2.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
        public ushort MaxPoints;              // +2

        /// <summary>Gets or sets the maximum contours in a non-composite glyph.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxContours</see> occupies offset 4.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
        public ushort MaxContours;            // +4

        /// <summary>Gets or sets the maximum points in a composite glyph.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxCompositePoints</see> occupies offset 6.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
        public ushort MaxCompositePoints;     // +6

        /// <summary>Gets or sets the maximum contours in a composite glyph.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxCompositeContours</see> occupies offset 8.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
        public ushort MaxCompositeContours;   // +8

        /// <summary>Gets or sets the maximum number of zones.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxZones</see> occupies offset 10.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
        public ushort MaxZones;               // +10

        /// <summary>Gets or sets the maximum number of twilight-zone points.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxTwilightPoints</see> occupies offset 12.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
        public ushort MaxTwilightPoints;      // +12

        /// <summary>Gets or sets the number of Storage Area locations.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxStorage</see> occupies offset 14.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
        public ushort MaxStorage;             // +14

        /// <summary>Gets or sets the number of function definitions.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxFunctionDefs</see> occupies offset 16.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
        public ushort MaxFunctionDefs;        // +16

        /// <summary>Gets or sets the number of instruction definitions.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxInstructionDefs</see> occupies offset 18.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
        public ushort MaxInstructionDefs;     // +18

        /// <summary>Gets or sets the maximum stack depth.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxStackElements</see> occupies offset 20.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
        public ushort MaxStackElements;       // +20

        /// <summary>Gets or sets the maximum byte count of glyph instructions.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxSizeOfInstructions</see> occupies offset 22.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
        public ushort MaxSizeOfInstructions;  // +22

        /// <summary>Gets or sets the maximum number of top-level component elements.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxComponentElements</see> occupies offset 24.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
        public ushort MaxComponentElements;   // +24

        /// <summary>Gets or sets the maximum component recursion depth.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">maxComponentDepth</see> occupies offset 26.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
        public ushort MaxComponentDepth;      // +26

        /// <inheritdoc/>
        public static Header10 ReverseEndianness(Header10 v) => new()
        {
            NumGlyphs = BinaryPrimitives.ReverseEndianness(v.NumGlyphs),
            MaxPoints = BinaryPrimitives.ReverseEndianness(v.MaxPoints),
            MaxContours = BinaryPrimitives.ReverseEndianness(v.MaxContours),
            MaxCompositePoints = BinaryPrimitives.ReverseEndianness(v.MaxCompositePoints),
            MaxCompositeContours = BinaryPrimitives.ReverseEndianness(v.MaxCompositeContours),
            MaxZones = BinaryPrimitives.ReverseEndianness(v.MaxZones),
            MaxTwilightPoints = BinaryPrimitives.ReverseEndianness(v.MaxTwilightPoints),
            MaxStorage = BinaryPrimitives.ReverseEndianness(v.MaxStorage),
            MaxFunctionDefs = BinaryPrimitives.ReverseEndianness(v.MaxFunctionDefs),
            MaxInstructionDefs = BinaryPrimitives.ReverseEndianness(v.MaxInstructionDefs),
            MaxStackElements = BinaryPrimitives.ReverseEndianness(v.MaxStackElements),
            MaxSizeOfInstructions = BinaryPrimitives.ReverseEndianness(v.MaxSizeOfInstructions),
            MaxComponentElements = BinaryPrimitives.ReverseEndianness(v.MaxComponentElements),
            MaxComponentDepth = BinaryPrimitives.ReverseEndianness(v.MaxComponentDepth),
        };
    }
}

/// <summary>The version of the <c>maxp</c> table.</summary>
/// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp">OpenType maxp — Maximum Profile</see> defines version 0.5 for CFF outlines and version 1.0 for TrueType outlines.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp"/>
public enum MaxpVersion : uint
{
    /// <summary>Version 0.5 (0x00005000), used with CFF outlines.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00005000">Version 0.5</see> contains only <c>numGlyphs</c>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00005000"/>
    Version05 = 0x00005000,

    /// <summary>Version 1.0 (0x00010000), used with TrueType outlines.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000">Version 1.0</see> contains the complete maximum-profile field set.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp#version-0x00010000"/>
    Version10 = 0x00010000,
}
