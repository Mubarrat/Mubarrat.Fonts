using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>name</c> table: multilingual strings associated with the font.</summary>
/// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name">OpenType name — Naming Table</see> defines name records keyed by platform, encoding, language, and name ID. Version 0 uses platform-specific language IDs; version 1 additionally supports language-tag strings. Strings are decoded from their on-disk representation during parsing and exposed as <see cref="NameRecord.String"/>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name"/>
public sealed record NameTable : IFontTable<NameTable>
{
    /// <summary>Gets the language ID at which language-tag entries begin in a version 1 table.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records">Language-tag records</see> use IDs beginning at <c>0x8000</c>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
    public const ushort LangTagBase = 0x8000;

    /// <inheritdoc/>
    public static Tag Tag => "name";

    /// <summary>Gets the table version.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name">The name table</see> currently defines versions 0 and 1.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name"/>
    public ushort Version { get; init; }

    /// <summary>Gets the name records in table order.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records">Name records</see> contain the platform, encoding, language, name ID, and string data for each entry.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
    public IReadOnlyList<NameRecord> Records { get; init; } = [];

    /// <summary>Gets the language-tag strings from a version 1 table.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records">Language-tag records</see> are indexed so that entry <c>i</c> corresponds to language ID <see cref="LangTagBase"/> + <c>i</c>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
    public IReadOnlyList<string> LanguageTags { get; init; } = [];

    /// <summary>Gets the number of name records.</summary>
    /// <remarks>This is the number of entries represented by <see cref="Records"/>.</remarks>
    public int Count => Records.Count;

    /// <summary>Gets the number of language-tag strings.</summary>
    /// <remarks>This is the number of entries represented by <see cref="LanguageTags"/>.</remarks>
    public int LangTagCount => LanguageTags.Count;

    /// <summary>Gets a value indicating whether the table is version 1 and may contain language-tag strings.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name">Version 1</see> adds language-tag records to the <c>name</c> table.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name"/>
    public bool HasLanguageTags => Version == 1;

    /// <summary>Parses a <c>name</c> table from the current cursor position.</summary>
    /// <param name="cursor">The cursor positioned at the beginning of the table.</param>
    /// <param name="context">The parsing context; unused by this table.</param>
    /// <returns>The parsed <see cref="NameTable"/>.</returns>
    /// <exception cref="InvalidDataException">The table version is neither 0 nor 1.</exception>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name">The name table</see> stores fixed-layout records followed by string storage. The parser resolves each record's string offset and length immediately and exposes only the decoded string.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name"/>
    public static NameTable Parse(ref Cursor cursor, object? context)
    {
        // name is a leaf: parent is unused.
        Source source = cursor.Source;

        ushort version = cursor.ReadUInt16();
        if (version is not (0 or 1))
            throw new InvalidDataException(
                $"'name'.version is {version}, expected 0 or 1.");

        ushort count = cursor.ReadUInt16();
        ushort storageOffset = cursor.ReadUInt16();

        // Read the raw on-disk records as a struct array in one pass. The struct contains
        // the (length, offset) pair that points into the string storage area; that pair is
        // resolved immediately below and never escapes this method.
        var rawRecords = cursor.ReadBigEndianStructArray<RawNameRecord>(count);

        var records = new NameRecord[count];
        for (int i = 0; i < count; i++)
        {
            ref readonly var raw = ref rawRecords[i];
            records[i] = new NameRecord
            {
                PlatformId = (NamePlatformId)raw.PlatformId,
                EncodingId = raw.EncodingId,
                LanguageId = raw.LanguageId,
                NameId = (NameId)raw.NameId,
                String = raw.Length == 0
                    ? string.Empty
                    : DecodeString(source, storageOffset + raw.StringOffset, raw.Length),
            };
        }

        var languageTags = Array.Empty<string>();
        if (version == 1)
        {
            ushort langTagCount = cursor.ReadUInt16();
            if (langTagCount > 0)
            {
                var rawTags = cursor.ReadBigEndianStructArray<RawLangTagRecord>(langTagCount);

                languageTags = new string[langTagCount];
                for (int i = 0; i < langTagCount; i++)
                {
                    ref readonly var raw = ref rawTags[i];
                    languageTags[i] = raw.Length == 0
                        ? string.Empty
                        : DecodeString(source, storageOffset + raw.Offset, raw.Length);
                }
            }
        }

        return new NameTable
        {
            Version = version,
            Records = records,
            LanguageTags = languageTags,
        };
    }

    /// <summary>Gets the language-tag string for <paramref name="languageId"/>, or <c>null</c> when no corresponding entry exists.</summary>
    /// <param name="languageId">The language ID to resolve.</param>
    /// <returns>The corresponding language-tag string, or <c>null</c> when <paramref name="languageId"/> is below <see cref="LangTagBase"/> or has no corresponding entry.</returns>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records">Language-tag IDs</see> begin at <c>0x8000</c> in version 1.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
    public string? GetLanguageTag(ushort languageId)
    {
        if (languageId < LangTagBase) return null;
        int index = languageId - LangTagBase;
        return index < LanguageTags.Count ? LanguageTags[index] : null;
    }

    /// <summary>Attempts to get the language-tag string for <paramref name="languageId"/>.</summary>
    /// <param name="languageId">The language ID to resolve.</param>
    /// <param name="tag">Receives the corresponding language-tag string, or <see cref="string.Empty"/> when none exists.</param>
    /// <returns><c>true</c> when a corresponding language-tag string exists; otherwise, <c>false</c>.</returns>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records">Language-tag IDs</see> begin at <c>0x8000</c> in version 1.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
    public bool TryGetLanguageTag(ushort languageId, out string tag)
    {
        var result = GetLanguageTag(languageId);
        tag = result ?? string.Empty;
        return result is not null;
    }

    /// <summary>Finds the first record matching <paramref name="nameId"/>, <paramref name="platformId"/>, and <paramref name="languageId"/>.</summary>
    /// <param name="nameId">The name ID to match.</param>
    /// <param name="platformId">The platform ID to match.</param>
    /// <param name="languageId">The language ID to match.</param>
    /// <returns>The first matching <see cref="NameRecord"/>, or <c>null</c> when no record matches.</returns>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records">Name records</see> are identified by platform, encoding, language, and name ID; this overload intentionally ignores encoding.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
    public NameRecord? Find(NameId nameId, NamePlatformId platformId, ushort languageId)
    {
        for (int i = 0; i < Records.Count; i++)
        {
            var r = Records[i];
            if (r.NameId == nameId && r.PlatformId == platformId && r.LanguageId == languageId)
                return r;
        }
        return null;
    }

    /// <summary>Finds the first record matching <paramref name="nameId"/> for any platform and language.</summary>
    /// <param name="nameId">The name ID to match.</param>
    /// <returns>The first matching <see cref="NameRecord"/>, or <c>null</c> when no record matches.</returns>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records">Name IDs</see> identify logical string categories independently of platform-specific records.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
    public NameRecord? Find(NameId nameId)
    {
        for (int i = 0; i < Records.Count; i++)
            if (Records[i].NameId == nameId)
                return Records[i];
        return null;
    }

    /// <summary>Gets the string for the first record matching the specified name, platform, and language.</summary>
    /// <param name="nameId">The name ID to match.</param>
    /// <param name="platformId">The platform ID to match.</param>
    /// <param name="languageId">The language ID to match.</param>
    /// <returns>The decoded string, or <c>null</c> when no matching record exists.</returns>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records">Name records</see> associate a decoded string with a name ID, platform, encoding, and language.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
    public string? GetString(NameId nameId, NamePlatformId platformId, ushort languageId) =>
        Find(nameId, platformId, languageId)?.String;

    /// <summary>Gets the first string matching <paramref name="nameId"/> using the parser's platform preference order.</summary>
    /// <param name="nameId">The name ID to match.</param>
    /// <returns>The selected decoded string, or <c>null</c> when no matching record exists.</returns>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records">Platform-specific name records</see> may provide multiple representations of the same logical name; this implementation prefers Windows Unicode BMP, then any Windows record, then Macintosh Roman.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
    public string? GetString(NameId nameId)
    {
        for (int i = 0; i < Records.Count; i++)
        {
            var r = Records[i];
            if (r.NameId == nameId && r.PlatformId == NamePlatformId.Windows && r.EncodingId == 1)
                return r.String;
        }
        for (int i = 0; i < Records.Count; i++)
        {
            var r = Records[i];
            if (r.NameId == nameId && r.PlatformId == NamePlatformId.Windows)
                return r.String;
        }
        for (int i = 0; i < Records.Count; i++)
        {
            var r = Records[i];
            if (r.NameId == nameId && r.PlatformId == NamePlatformId.Macintosh && r.EncodingId == 0)
                return r.String;
        }
        return null;
    }

    /// <summary>Compares two name-record keys by platform, encoding, language, and name ID.</summary>
    /// <param name="a">The first name record.</param>
    /// <param name="b">The second name record.</param>
    /// <returns>A value less than zero when <paramref name="a"/> precedes <paramref name="b"/>, zero when their keys are equal, or a value greater than zero when <paramref name="a"/> follows <paramref name="b"/>.</returns>
    /// <remarks>The comparison order corresponds to the tuple <c>(PlatformId, EncodingId, LanguageId, NameId)</c>.</remarks>
    public static int CompareKey(in NameRecord a, in NameRecord b)
    {
        int c;
        if ((c = ((int)a.PlatformId).CompareTo((int)b.PlatformId)) != 0) return c;
        if ((c = a.EncodingId.CompareTo(b.EncodingId)) != 0) return c;
        if ((c = a.LanguageId.CompareTo(b.LanguageId)) != 0) return c;
        return ((int)a.NameId).CompareTo((int)b.NameId);
    }

    /// <summary>Decodes a name-table string from UTF-16BE storage.</summary>
    /// <param name="source">The source containing the string bytes.</param>
    /// <param name="offset">The byte offset of the string within <paramref name="source"/>.</param>
    /// <param name="length">The byte length of the string.</param>
    /// <returns>The decoded string.</returns>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records">String storage</see> is decoded according to the platform and encoding represented by the name record; this implementation decodes the supplied bytes as UTF-16BE.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
    public static string DecodeString(Source source, long offset, int length)
    {
        if (length == 0) return string.Empty;

        Span<byte> bytes = length <= 512
            ? stackalloc byte[length]
            : new byte[length];

        source.ReadUInt8ArrayAt(offset, bytes);
        return Encoding.BigEndianUnicode.GetString(bytes);
    }

    /// <summary>The on-disk layout of a name record.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records">Name records</see> are 12-byte fixed-layout records containing platform ID, encoding ID, language ID, name ID, string length, and string offset. The offset and length are resolved during parsing and are not exposed through <see cref="NameRecord"/>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct RawNameRecord : IEndianReversibleStruct<RawNameRecord>
    {
        /// <summary>The platform ID at offset 0.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
        public ushort PlatformId;

        /// <summary>The platform-specific encoding ID at offset 2.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
        public ushort EncodingId;

        /// <summary>The language ID at offset 4.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
        public ushort LanguageId;

        /// <summary>The name ID at offset 6.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
        public ushort NameId;

        /// <summary>The string length in bytes at offset 8.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
        public ushort Length;

        /// <summary>The string offset from the beginning of the string storage area at offset 10.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
        public ushort StringOffset;

        /// <inheritdoc/>
        public static RawNameRecord ReverseEndianness(RawNameRecord value) => new()
        {
            PlatformId = BinaryPrimitives.ReverseEndianness(value.PlatformId),
            EncodingId = BinaryPrimitives.ReverseEndianness(value.EncodingId),
            LanguageId = BinaryPrimitives.ReverseEndianness(value.LanguageId),
            NameId = BinaryPrimitives.ReverseEndianness(value.NameId),
            Length = BinaryPrimitives.ReverseEndianness(value.Length),
            StringOffset = BinaryPrimitives.ReverseEndianness(value.StringOffset),
        };
    }

    /// <summary>The on-disk layout of a language-tag record.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records">Language-tag records</see> contain a 16-bit string length followed by a 16-bit offset into the table's string storage area.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct RawLangTagRecord : IEndianReversibleStruct<RawLangTagRecord>
    {
        /// <summary>The language-tag string length in bytes at offset 0.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
        public ushort Length;

        /// <summary>The language-tag string offset at offset 2.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
        public ushort Offset;

        /// <inheritdoc/>
        public static RawLangTagRecord ReverseEndianness(RawLangTagRecord value) => new()
        {
            Length = BinaryPrimitives.ReverseEndianness(value.Length),
            Offset = BinaryPrimitives.ReverseEndianness(value.Offset),
        };
    }
}

/// <summary>A fully parsed entry in the <c>name</c> table with its decoded string value.</summary>
/// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records">Name records</see> identify strings by platform, encoding, language, and name ID; this semantic representation replaces the on-disk string length and offset with the decoded <see cref="String"/>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
public record struct NameRecord : IEquatable<NameRecord>
{
    /// <summary>Gets or sets the platform ID.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#platform-ids">Platform IDs</see> identify the platform associated with the name record.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#platform-ids"/>
    public NamePlatformId PlatformId;

    /// <summary>Gets or sets the platform-specific encoding ID.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records">Encoding IDs</see> identify the encoding associated with the selected platform.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
    public ushort EncodingId;

    /// <summary>Gets or sets the language ID.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records">Language IDs</see> identify the language of the name string; version 1 may use IDs beginning at <see cref="NameTable.LangTagBase"/> for language tags.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
    public ushort LanguageId;

    /// <summary>Gets or sets the name ID identifying the logical string category.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids">Name IDs</see> identify standard and font-specific naming categories.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    public NameId NameId;

    /// <summary>Gets or sets the decoded string value.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records">String storage</see> is resolved during parsing and is exposed here as a managed string.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-records"/>
    public string String;

    /// <inheritdoc/>
    public readonly override string ToString() =>
        $"({PlatformId}, enc {EncodingId}, lang 0x{LanguageId:X4}, {NameId}) = \"{String}\"";
}

/// <summary>Platform IDs used by <c>name</c> records.</summary>
/// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#platform-ids">Platform IDs</see> identify the platform associated with a name record and are also used by <c>cmap</c>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#platform-ids"/>
public enum NamePlatformId : ushort
{
    /// <summary>Unicode platform.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#platform-ids">Platform 0</see> is the Unicode platform.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#platform-ids"/>
    Unicode = 0,

    /// <summary>Macintosh platform.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#platform-ids">Platform 1</see> is the Macintosh platform.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#platform-ids"/>
    Macintosh = 1,

    /// <summary>ISO platform.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#platform-ids">Platform 2</see> is deprecated.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#platform-ids"/>
    Iso = 2,

    /// <summary>Windows platform.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#platform-ids">Platform 3</see> is the Windows platform; encoding 1 represents Unicode BMP and encoding 10 represents the Unicode full repertoire.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#platform-ids"/>
    Windows = 3,

    /// <summary>Custom platform.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#platform-ids">Platform 4</see> is reserved for custom platform-specific use.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#platform-ids"/>
    Custom = 4,
}

/// <summary>Predefined name IDs for the <c>name</c> table.</summary>
/// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids">Name IDs</see> 0–25 define standard naming categories; IDs 26–255 are reserved for future standard names and IDs 256–32767 are reserved for font-specific names.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
public enum NameId : ushort
{
    /// <summary>Copyright notice.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    Copyright = 0,

    /// <summary>Font Family name.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids">Name ID 1</see> identifies the font family name; extended families may also use <see cref="TypographicFamily"/>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    FontFamily = 1,

    /// <summary>Font Subfamily name.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids">Name ID 2</see> identifies the font subfamily name.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    FontSubfamily = 2,

    /// <summary>Unique font identifier.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    UniqueId = 3,

    /// <summary>Full font name.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    FullName = 4,

    /// <summary>Version string, conventionally beginning with <c>Version </c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    Version = 5,

    /// <summary>PostScript name.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    PostScriptName = 6,

    /// <summary>Trademark notice.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    Trademark = 7,

    /// <summary>Manufacturer name.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    Manufacturer = 8,

    /// <summary>Designer name.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    Designer = 9,

    /// <summary>Description of the font.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    Description = 10,

    /// <summary>Vendor URL.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    VendorUrl = 11,

    /// <summary>Designer URL.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    DesignerUrl = 12,

    /// <summary>License description.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    License = 13,

    /// <summary>License information URL.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    LicenseUrl = 14,

    /// <summary>Reserved; must not be used.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    Reserved15 = 15,

    /// <summary>Typographic Family name.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids">Name ID 16</see> identifies the preferred typographic family name for extended families.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    TypographicFamily = 16,

    /// <summary>Typographic Subfamily name.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids">Name ID 17</see> identifies the preferred typographic subfamily name.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    TypographicSubfamily = 17,

    /// <summary>Compatible Full name.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids">Name ID 18</see> is intended for Macintosh compatibility.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    CompatibleFull = 18,

    /// <summary>Sample text.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    SampleText = 19,

    /// <summary>PostScript CID findfont name.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    PostScriptCidName = 20,

    /// <summary>WWS Family name.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    WwsFamily = 21,

    /// <summary>WWS Subfamily name.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    WwsSubfamily = 22,

    /// <summary>Light background palette.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    LightBackgroundPalette = 23,

    /// <summary>Dark background palette.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    DarkBackgroundPalette = 24,

    /// <summary>Variations PostScript name prefix.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    VariationsPostScriptNamePrefix = 25,
}
