using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType.Tables;

/// <summary>The <c>post</c> table: additional information needed to use OpenType fonts on PostScript printers, including the italic angle, underline metrics, and (for version 2.0) the PostScript names of all glyphs.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Versions 1.0 and 3.0 are fixed-size (32 bytes) and contain no glyph names.</description></item>
/// <item><description>Version 2.0 appends a glyph-name index array and a Pascal-string data block.</description></item>
/// <item><description>Version 2.5 (deprecated) appends a signed byte offset per glyph.</description></item>
/// </list>
/// <para>Version 2.0 glyph names are fully materialized during parsing: a glyph's name is resolved to a <see cref="string"/> at parse time, so no <see cref="Source"/> reference is retained. The 258 standard Macintosh glyph names are used for indices 0–257; indices 258 and above index into the table's custom string data.</para>
/// <para>Version 3.0 is the normal choice for fonts with CFF version 1 outlines, and is common for TrueType fonts that do not need PostScript glyph names.</para>
/// <para>For the table layout, see <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post">the <c>post</c> table</see> in the OpenType specification.</para>
/// </remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post">OpenType specification: <c>post</c> table</seealso>
/// <seealso cref="PostVersion"/>
/// <seealso cref="Header"/>
/// <seealso cref="MacintoshGlyphNames"/>
public sealed record PostTable : IOpenTypeTable<PostTable>
{
    /// <inheritdoc/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post">OpenType specification: <c>post</c> table</seealso>
    public static Tag Tag => "post";

    /// <summary>Gets the table version.</summary>
    /// <value>One of the four <see cref="PostVersion"/> values defined by the specification.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#header"><c>version</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="PostVersion"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#header">OpenType specification: <c>post</c>, <c>version</c></seealso>
    public PostVersion Version { get; init; }

    /// <summary>Gets the italic angle in counter-clockwise degrees from the vertical.</summary>
    /// <value>Zero for upright text; negative for text that leans to the right (forward).</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#italicangle"><c>italicAngle</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#italicangle">OpenType specification: <c>post</c>, <c>italicAngle</c></seealso>
    public Fixed ItalicAngle { get; init; }

    /// <summary>Gets the suggested y-coordinate of the top of the underline.</summary>
    /// <value>The distance from the baseline to the <em>top</em> of the underline, in font design units.</value>
    /// <remarks>
    /// <para>This is not the same as the PostScript <c>UnderlinePosition</c> FontInfo key, which specifies the distance from the baseline to the <em>center</em> of the underline.</para>
    /// <para>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#underlineposition"><c>underlinePosition</c> field</see> in the OpenType specification.</para>
    /// </remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#underlineposition">OpenType specification: <c>post</c>, <c>underlinePosition</c></seealso>
    public short UnderlinePosition { get; init; }

    /// <summary>Gets the suggested underline thickness.</summary>
    /// <value>The thickness of the underline, in font design units.</value>
    /// <remarks>
    /// <para>In general this should match the thickness of the underscore character and the strikeout thickness from the <c>OS/2</c> table.</para>
    /// <para>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#underlinethickness"><c>underlineThickness</c> field</see> in the OpenType specification.</para>
    /// </remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#underlinethickness">OpenType specification: <c>post</c>, <c>underlineThickness</c></seealso>
    public short UnderlineThickness { get; init; }

    /// <summary>Gets the raw <c>isFixedPitch</c> value.</summary>
    /// <value><c>0</c> if the font is proportionally spaced; non-zero if it is monospaced.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#isfixedpitch"><c>isFixedPitch</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="IsMonospaced"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#isfixedpitch">OpenType specification: <c>post</c>, <c>isFixedPitch</c></seealso>
    public uint IsFixedPitch { get; init; }

    /// <summary>Gets the minimum memory usage when an OpenType font is downloaded.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#minmemtype42"><c>minMemType42</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#minmemtype42">OpenType specification: <c>post</c>, <c>minMemType42</c></seealso>
    public uint MinMemType42 { get; init; }

    /// <summary>Gets the maximum memory usage when an OpenType font is downloaded.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#maxmemtype42"><c>maxMemType42</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#maxmemtype42">OpenType specification: <c>post</c>, <c>maxMemType42</c></seealso>
    public uint MaxMemType42 { get; init; }

    /// <summary>Gets the minimum memory usage when the font is downloaded as a Type 1 font.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#minmemtype1"><c>minMemType1</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#minmemtype1">OpenType specification: <c>post</c>, <c>minMemType1</c></seealso>
    public uint MinMemType1 { get; init; }

    /// <summary>Gets the maximum memory usage when the font is downloaded as a Type 1 font.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#maxmemtype1"><c>maxMemType1</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#maxmemtype1">OpenType specification: <c>post</c>, <c>maxMemType1</c></seealso>
    public uint MaxMemType1 { get; init; }

    /// <summary>Gets the number of glyphs.</summary>
    /// <value>The glyph count for version 2.0 and 2.5; zero for versions 1.0 and 3.0.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-20"><c>numGlyphs</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-20">OpenType specification: <c>post</c>, version 2.0 <c>numGlyphs</c></seealso>
    public ushort NumGlyphs { get; init; }

    /// <summary>Gets the resolved PostScript glyph names for version 2.0, indexed by glyph ID.</summary>
    /// <value>An array of length <see cref="NumGlyphs"/>; <c>null</c> for versions 1.0, 2.5, and 3.0.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-20"><c>glyphNameIndex</c> and string data fields</see> in the OpenType specification.</remarks>
    /// <seealso cref="HasGlyphNames"/>
    /// <seealso cref="GetGlyphName"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-20">OpenType specification: <c>post</c>, version 2.0</seealso>
    public IReadOnlyList<string>? GlyphNames { get; init; }

    /// <summary>Gets the signed byte offsets for version 2.5, indexed by glyph ID.</summary>
    /// <value>An array where each byte is the difference between the glyph index used in the font and the standard Macintosh glyph index; <c>null</c> for versions 1.0, 2.0, and 3.0.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-25-deprecated"><c>offset</c> array</see> in the OpenType specification.</remarks>
    /// <seealso cref="HasGlyphNameOffsets"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-25-deprecated">OpenType specification: <c>post</c>, version 2.5 (deprecated)</seealso>
    public IReadOnlyList<sbyte>? GlyphNameOffsets { get; init; }

    /// <summary>Gets a value indicating whether the font is monospaced.</summary>
    /// <value><see langword="true"/> when <see cref="IsFixedPitch"/> is non-zero.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#isfixedpitch"><c>isFixedPitch</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="IsFixedPitch"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#isfixedpitch">OpenType specification: <c>post</c>, <c>isFixedPitch</c></seealso>
    public bool IsMonospaced => IsFixedPitch != 0;

    /// <summary>Gets a value indicating whether the table contains PostScript glyph names.</summary>
    /// <value><see langword="true"/> for version 2.0 only.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-20">version 2.0 field layout</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphNames"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-20">OpenType specification: <c>post</c>, version 2.0</seealso>
    public bool HasGlyphNames => Version == PostVersion.Version20;

    /// <summary>Gets a value indicating whether the table provides byte offsets into the Macintosh glyph set.</summary>
    /// <value><see langword="true"/> for version 2.5 only.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-25-deprecated">version 2.5 field layout</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphNameOffsets"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-25-deprecated">OpenType specification: <c>post</c>, version 2.5 (deprecated)</seealso>
    public bool HasGlyphNameOffsets => Version == PostVersion.Version25;

    /// <summary>Returns the PostScript name of glyph <paramref name="glyphId"/>.</summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><description>Version 2.0 — the glyph name from <see cref="GlyphNames"/>.</description></item>
    /// <item><description>Version 1.0 — the standard Macintosh name at that index (valid only for the 258 standard glyphs).</description></item>
    /// <item><description>Versions 2.5 and 3.0 — <see cref="string.Empty"/>, because no names are stored.</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is negative.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-20">glyph name resolution rules</see> in the OpenType specification.</remarks>
    /// <example>
    /// <code>
    /// var table = font.GetTable&lt;PostTable&gt;();
    /// string name = table.GetGlyphName(42);
    /// </code>
    /// </example>
    /// <seealso cref="GlyphNames"/>
    /// <seealso cref="MacintoshGlyphNames"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-20">OpenType specification: <c>post</c>, version 2.0</seealso>
    public string GetGlyphName(int glyphId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);

        if (Version == PostVersion.Version20 && GlyphNames is { } names)
            return glyphId < names.Count ? names[glyphId] : string.Empty;

        if (Version == PostVersion.Version10)
            return glyphId < MacintoshGlyphNames.Length ? MacintoshGlyphNames[glyphId] : string.Empty;

        return string.Empty;
    }

    /// <inheritdoc/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#header">OpenType specification: <c>post</c> header</seealso>
    static PostTable IRecord<PostTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        var version = (PostVersion)header.Version;

        return version switch
        {
            PostVersion.Version10 or PostVersion.Version30 => new PostTable
            {
                Version = version,
                ItalicAngle = header.ItalicAngle,
                UnderlinePosition = header.UnderlinePosition,
                UnderlineThickness = header.UnderlineThickness,
                IsFixedPitch = header.IsFixedPitch,
                MinMemType42 = header.MinMemType42,
                MaxMemType42 = header.MaxMemType42,
                MinMemType1 = header.MinMemType1,
                MaxMemType1 = header.MaxMemType1,
            },

            PostVersion.Version20 => ParseVersion20(ref cursor, in header),

            PostVersion.Version25 => ParseVersion25(ref cursor, in header),

            _ => throw new InvalidDataException($"'post'.version is 0x{header.Version:X8}, expected 0x00010000, 0x00020000, 0x00025000, or 0x00030000."),
        };
    }

    /// <summary>Parses the version 2.0 body of a <c>post</c> table: a <c>numGlyphs</c> field, a <c>glyphNameIndex</c> array, and a Pascal-string data block.</summary>
    /// <param name="cursor">The cursor positioned immediately after the 32-byte header.</param>
    /// <param name="header">The already-read table header, carried through to the returned record.</param>
    /// <returns>A <see cref="PostTable"/> with <see cref="GlyphNames"/> fully materialized.</returns>
    /// <exception cref="EndOfStreamException">The read extends past the end of the underlying <see cref="Source"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-20">version 2.0 field layout</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-20">OpenType specification: <c>post</c>, version 2.0</seealso>
    public static PostTable ParseVersion20(ref Cursor cursor, in Header header)
    {
        ushort numGlyphs = cursor.ReadUInt16();

        ushort[] nameIndices = cursor.ReadUInt16Array(numGlyphs);

        int maxIndex = 0;
        for (int i = 0; i < nameIndices.Length; i++)
            if (nameIndices[i] > maxIndex) maxIndex = nameIndices[i];

        string[] customNames = maxIndex >= 258
            ? ReadCustomNames(ref cursor, maxIndex - 258 + 1)
            : [];

        var names = new string[numGlyphs];
        for (int i = 0; i < numGlyphs; i++)
        {
            int index = nameIndices[i];
            names[i] = index < 258
                ? MacintoshGlyphNames[index]
                : customNames[index - 258];
        }

        return new PostTable
        {
            Version = PostVersion.Version20,
            ItalicAngle = header.ItalicAngle,
            UnderlinePosition = header.UnderlinePosition,
            UnderlineThickness = header.UnderlineThickness,
            IsFixedPitch = header.IsFixedPitch,
            MinMemType42 = header.MinMemType42,
            MaxMemType42 = header.MaxMemType42,
            MinMemType1 = header.MinMemType1,
            MaxMemType1 = header.MaxMemType1,
            NumGlyphs = numGlyphs,
            GlyphNames = names,
        };
    }

    /// <summary>Parses the version 2.5 body of a <c>post</c> table: a <c>numGlyphs</c> field followed by one signed byte offset per glyph.</summary>
    /// <param name="cursor">The cursor positioned immediately after the 32-byte header.</param>
    /// <param name="header">The already-read table header, carried through to the returned record.</param>
    /// <returns>A <see cref="PostTable"/> with <see cref="GlyphNameOffsets"/> populated.</returns>
    /// <exception cref="EndOfStreamException">The read extends past the end of the underlying <see cref="Source"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-25-deprecated">version 2.5 field layout</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-25-deprecated">OpenType specification: <c>post</c>, version 2.5 (deprecated)</seealso>
    public static PostTable ParseVersion25(ref Cursor cursor, in Header header)
    {
        sbyte[] offsets = cursor.ReadInt8Array(cursor.ReadUInt16());

        return new PostTable
        {
            Version = PostVersion.Version25,
            ItalicAngle = header.ItalicAngle,
            UnderlinePosition = header.UnderlinePosition,
            UnderlineThickness = header.UnderlineThickness,
            IsFixedPitch = header.IsFixedPitch,
            MinMemType42 = header.MinMemType42,
            MaxMemType42 = header.MaxMemType42,
            MinMemType1 = header.MinMemType1,
            MaxMemType1 = header.MaxMemType1,
            NumGlyphs = cursor.ReadUInt16(),
            GlyphNameOffsets = offsets,
        };
    }

    /// <summary>Reads <paramref name="count"/> Pascal strings from the cursor. Each string is a one-byte length followed by that many ASCII bytes.</summary>
    /// <param name="cursor">The cursor positioned at the first Pascal string.</param>
    /// <param name="count">The number of strings to read.</param>
    /// <returns>An array of <paramref name="count"/> strings; missing entries are <see cref="string.Empty"/>.</returns>
    /// <remarks>
    /// <para>If the cursor has no remaining bytes, the returned array is filled with <see cref="string.Empty"/>. Entries whose declared length exceeds the remaining buffer are truncated.</para>
    /// <para>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-20">string data block</see> in the OpenType specification.</para>
    /// </remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-20">OpenType specification: <c>post</c>, version 2.0 string data</seealso>
    public static string[] ReadCustomNames(ref Cursor cursor, int count)
    {
        int available = checked((int)cursor.Remaining);
        if (available <= 0) return new string[count];

        byte[] data = new byte[available];
        cursor.ReadBytes(data);

        var names = new string[count];
        int at = 0;
        for (int i = 0; i < count; i++)
        {
            if (at >= data.Length)
            {
                names[i] = string.Empty;
                continue;
            }

            int length = data[at++];
            int end = Math.Min(at + length, data.Length);
            names[i] = System.Text.Encoding.ASCII.GetString(data, at, end - at);
            at = end;
        }
        return names;
    }

    /// <summary>The 32-byte <c>post</c> table header. Blittable, no padding.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#header"><c>post</c> header layout</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#header">OpenType specification: <c>post</c> header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>The table version as a raw <c>Version16Dot16</c> value.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#header"><c>version</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#header">OpenType specification: <c>post</c>, <c>version</c></seealso>
        public uint Version;

        /// <summary>The italic angle as a 16.16 fixed-point value.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#italicangle"><c>italicAngle</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#italicangle">OpenType specification: <c>post</c>, <c>italicAngle</c></seealso>
        public Fixed ItalicAngle;

        /// <summary>The suggested underline position.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#underlineposition"><c>underlinePosition</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#underlineposition">OpenType specification: <c>post</c>, <c>underlinePosition</c></seealso>
        public short UnderlinePosition;

        /// <summary>The suggested underline thickness.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#underlinethickness"><c>underlineThickness</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#underlinethickness">OpenType specification: <c>post</c>, <c>underlineThickness</c></seealso>
        public short UnderlineThickness;

        /// <summary>The raw <c>isFixedPitch</c> value.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#isfixedpitch"><c>isFixedPitch</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#isfixedpitch">OpenType specification: <c>post</c>, <c>isFixedPitch</c></seealso>
        public uint IsFixedPitch;

        /// <summary>The minimum memory usage for an OpenType download.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#minmemtype42"><c>minMemType42</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#minmemtype42">OpenType specification: <c>post</c>, <c>minMemType42</c></seealso>
        public uint MinMemType42;

        /// <summary>The maximum memory usage for an OpenType download.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#maxmemtype42"><c>maxMemType42</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#maxmemtype42">OpenType specification: <c>post</c>, <c>maxMemType42</c></seealso>
        public uint MaxMemType42;

        /// <summary>The minimum memory usage for a Type 1 download.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#minmemtype1"><c>minMemType1</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#minmemtype1">OpenType specification: <c>post</c>, <c>minMemType1</c></seealso>
        public uint MinMemType1;

        /// <summary>The maximum memory usage for a Type 1 download.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#maxmemtype1"><c>maxMemType1</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#maxmemtype1">OpenType specification: <c>post</c>, <c>maxMemType1</c></seealso>
        public uint MaxMemType1;

        /// <inheritdoc/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#header">OpenType specification: <c>post</c> header</seealso>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(value.Version),
            ItalicAngle = Fixed.ReverseEndianness(value.ItalicAngle),
            UnderlinePosition = BinaryPrimitives.ReverseEndianness(value.UnderlinePosition),
            UnderlineThickness = BinaryPrimitives.ReverseEndianness(value.UnderlineThickness),
            IsFixedPitch = BinaryPrimitives.ReverseEndianness(value.IsFixedPitch),
            MinMemType42 = BinaryPrimitives.ReverseEndianness(value.MinMemType42),
            MaxMemType42 = BinaryPrimitives.ReverseEndianness(value.MaxMemType42),
            MinMemType1 = BinaryPrimitives.ReverseEndianness(value.MinMemType1),
            MaxMemType1 = BinaryPrimitives.ReverseEndianness(value.MaxMemType1),
        };
    }

    /// <summary>The 258 standard Macintosh glyph names, indexed by the value stored in <c>glyphNameIndex</c> for indices 0–257.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-20">standard Macintosh glyph name list</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-20">OpenType specification: <c>post</c>, version 2.0 <c>glyphNameIndex</c></seealso>
    public static readonly string[] MacintoshGlyphNames =
    [
        ".notdef", ".null", "nonmarkingreturn", "space", "exclam", "quotedbl",
        "numbersign", "dollar", "percent", "ampersand", "quotesingle", "parenleft",
        "parenright", "asterisk", "plus", "comma", "hyphen", "period", "slash",
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight",
        "nine", "colon", "semicolon", "less",
        "equal", "greater", "question", "at", "A", "B", "C", "D", "E", "F", "G",
        "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U",
        "V", "W", "X", "Y", "Z", "bracketleft", "backslash", "bracketright",
        "asciicircum", "underscore", "grave", "a", "b", "c", "d", "e", "f", "g",
        "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u",
        "v", "w", "x", "y", "z", "braceleft", "bar", "braceright", "asciitilde",
        "Adieresis", "Aring", "Ccedilla", "Eacute", "Ntilde", "Odieresis",
        "Udieresis", "aacute", "agrave", "acircumflex", "adieresis", "atilde",
        "aring", "ccedilla", "eacute", "egrave", "ecircumflex", "edieresis",
        "iacute", "igrave", "icircumflex", "idieresis", "ntilde", "oacute",
        "ograve", "ocircumflex", "odieresis", "otilde", "uacute", "ugrave",
        "ucircumflex", "udieresis",
        "dagger", "degree", "cent", "sterling", "section", "bullet", "paragraph",
        "germandbls", "registered", "copyright", "trademark", "acute", "dieresis",
        "notequal", "AE", "Oslash", "infinity", "plusminus", "lessequal",
        "greaterequal", "yen", "mu", "partialdiff", "summation", "product", "pi",
        "integral", "ordfeminine", "ordmasculine", "Omega", "ae", "oslash",
        "questiondown", "exclamdown", "logicalnot", "radical", "florin",
        "approxequal", "Delta", "guillemotleft", "guillemotright", "ellipsis",
        "nonbreakingspace", "Agrave", "Atilde", "Otilde", "OE", "oe", "endash",
        "emdash", "quotedblleft", "quotedblright", "quoteleft", "quoteright",
        "divide", "lozenge", "ydieresis", "Ydieresis", "fraction", "currency",
        "guilsinglleft", "guilsinglright", "fi", "fl",
        "daggerdbl", "periodcentered", "quotesinglbase", "quotedblbase",
        "perthousand", "Acircumflex", "Ecircumflex", "Aacute", "Edieresis",
        "Egrave", "Iacute", "Icircumflex", "Idieresis", "Igrave", "Oacute",
        "Ocircumflex", "apple", "Ograve", "Uacute", "Ucircumflex", "Ugrave",
        "dotlessi", "circumflex", "tilde", "macron", "breve", "dotaccent",
        "ring", "cedilla", "hungarumlaut", "ogonek", "caron",
        "Lslash", "lslash", "Scaron", "scaron", "Zcaron", "zcaron", "brokenbar",
        "Eth", "eth", "Yacute", "yacute", "Thorn", "thorn", "minus", "multiply",
        "onesuperior", "twosuperior", "threesuperior", "onehalf", "onequarter",
        "threequarters", "franc", "Gbreve", "gbreve", "Idotaccent", "Scedilla",
        "scedilla", "Cacute", "cacute", "Ccaron", "ccaron", "dcroat",
    ];
}

/// <summary>Version identifiers for the <c>post</c> table.</summary>
/// <remarks>
/// <para>The four defined values are <c>0x00010000</c>, <c>0x00020000</c>, <c>0x00025000</c>, and <c>0x00030000</c>.</para>
/// <para>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#header"><c>version</c> field</see> in the OpenType specification.</para>
/// </remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#header">OpenType specification: <c>post</c>, <c>version</c></seealso>
public enum PostVersion : uint
{
    /// <summary>Version 1.0: the font contains exactly the 258 standard Macintosh glyphs.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#header">version 1.0 header</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#header">OpenType specification: <c>post</c>, version 1.0</seealso>
    Version10 = 0x00010000,

    /// <summary>Version 2.0: glyph names are supplied explicitly.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-20">version 2.0 field layout</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-20">OpenType specification: <c>post</c>, version 2.0</seealso>
    Version20 = 0x00020000,

    /// <summary>Version 2.5 (deprecated): a pure subset or reordering of the Macintosh glyph set.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-25-deprecated">version 2.5 field layout</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#version-25-deprecated">OpenType specification: <c>post</c>, version 2.5 (deprecated)</seealso>
    Version25 = 0x00025000,

    /// <summary>Version 3.0: no PostScript name information is provided.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#header">version 3.0 header</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/post#header">OpenType specification: <c>post</c>, version 3.0</seealso>
    Version30 = 0x00030000,
}
