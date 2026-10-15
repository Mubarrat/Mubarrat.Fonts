using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>feat</c> table: the Apple Advanced Typography feature name table. Describes the font's text features, their settings, and the <c>name</c> table indices for human-readable labels of both.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each feature is identified by a feature type from the Apple Font Feature Registry and carries a set of selectors — settings the user can choose. The table associates each feature and each setting with a <c>name</c> table index so an application can display a label.</description></item>
/// <item><description>Features are either exclusive (one setting active at a time) or non-exclusive (each setting independently on or off). For a non-exclusive feature, settings come in on/off pairs and only the on setting has an entry in the setting name array.</description></item>
/// <item><description>Feature type 39 is reserved for language-specific glyph selection and is interpreted together with the <c>ltag</c> table. Its selector values are one plus an index into that table.</description></item>
/// <item><description>The table is intimately connected with the metamorphosis (<c>morx</c>) table. Any default setting the feature flags declare should agree with what the <c>morx</c> state machines actually do when no feature is requested.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">feature name table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FeatureName"/>
/// <seealso cref="SettingName"/>
/// <seealso cref="FeatureFlags"/>
/// <!--seealso cref="MorxTable"/-->
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">TrueType Reference Manual: The <c>feat</c> table</seealso>
public sealed record FeatTable : IFontTable<FeatTable>, IRecord<FeatTable>
{
    /// <summary>The version value for the only defined feature name table format, 1.0.</summary>
    /// <value>The <see cref="Fixed"/> value whose raw bits are <c>0x00010000</c>.</value>
    /// <seealso cref="Version"/>
    public static readonly Fixed CurrentVersion = new(0x00010000);

    /// <summary>The mask that extracts the default setting index from the <c>featureFlags</c> field.</summary>
    /// <value>The constant <c>0x00FF</c>.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The Apple documentation describes the field as "sixteen bits" but publishes the mask <c>0x00FF</c>, which is eight bits. Implementations follow the mask.</description></item>
    /// <item><description>The value is only meaningful when <see cref="FeatureFlags.ExplicitDefaultSetting"/> is set.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FeatureFlags"/>
    /// <seealso cref="FeatureName.DefaultSettingIndex"/>
    public const ushort DefaultSettingIndexMask = 0x00FF;

    /// <summary>The feature type used for language-specific glyph selection, interpreted together with the <c>ltag</c> table.</summary>
    /// <value>The constant <c>39</c>.</value>
    /// <remarks>Feature 39 must always be marked as exclusive. A selector value of 0 means "no change"; other values are one plus an index into the <c>ltag</c> table.</remarks>
    /// <seealso cref="FeatureName.Feature"/>
    public const ushort LanguageFeature = 39;

    /// <inheritdoc/>
    public static Tag Tag => "feat";

    /// <summary>Gets the table version.</summary>
    /// <value>The <see cref="CurrentVersion"/> value for a conforming table.</value>
    /// <seealso cref="CurrentVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">TrueType Reference Manual: The <c>feat</c> table</seealso>
    public Fixed Version { get; init; }

    /// <summary>Gets the feature name entries, in the on-disk order.</summary>
    /// <value>The array of <see cref="FeatureName"/> entries. The specification requires the array to be sorted by <see cref="FeatureName.Feature"/>, but the parser does not enforce the ordering.</value>
    /// <remarks>Each entry carries its own settings array, resolved during parse from the entry's <c>settingTable</c> offset.</remarks>
    /// <seealso cref="FeatureName"/>
    /// <seealso cref="TryGetFeature(ushort, out FeatureName)"/>
    public IReadOnlyList<FeatureName> Features { get; init; } = [];

    /// <summary>Gets the number of feature entries.</summary>
    /// <value>The length of <see cref="Features"/>.</value>
    /// <seealso cref="Features"/>
    public int FeatureCount => Features.Count;

    /// <summary>Tries to find a feature by feature type.</summary>
    /// <param name="featureType">The feature type to look up.</param>
    /// <param name="feature">When this method returns <see langword="true"/>, contains the feature; otherwise, <c>null</c>.</param>
    /// <returns><see langword="true"/> when a feature with the given type is present; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup is linear over <see cref="Features"/>. The specification requires the array to be sorted by feature type, so a binary search would also be valid; the linear form is chosen for simplicity.</description></item>
    /// <item><description>When duplicate feature types are present — which the specification does not forbid — the first match in table order is returned.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Features"/>
    /// <seealso cref="FeatureName"/>
    public bool TryGetFeature(ushort featureType, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out FeatureName? feature)
    {
        foreach (var candidate in Features)
        {
            if (candidate.Feature == featureType)
            {
                feature = candidate;
                return true;
            }
        }

        feature = null;
        return false;
    }

    /// <summary>The 8-byte <c>feat</c> table header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by the feature name array, then by the setting name arrays, each at its own offset.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">feature name table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FeatTable"/>
    /// <seealso cref="FeatureNameRecord"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">TrueType Reference Manual: The <c>feat</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0, as a <c>Fixed</c> (16.16) value.</summary>
        /// <value>The <see cref="CurrentVersion"/> value for a conforming table.</value>
        public Fixed Version;             // +0

        /// <summary>The number of entries in the feature name array at byte offset 4.</summary>
        public ushort FeatureNameCount;   // +4

        /// <summary>The first reserved field at byte offset 6.</summary>
        /// <value>The constant <c>0</c> for a conforming table.</value>
        public ushort Reserved1;          // +6

        /// <summary>The second reserved field at byte offset 8.</summary>
        /// <value>The constant <c>0</c> for a conforming table.</value>
        public uint Reserved2;            // +8

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = Fixed.ReverseEndianness(value.Version),
            FeatureNameCount = BinaryPrimitives.ReverseEndianness(value.FeatureNameCount),
            Reserved1 = BinaryPrimitives.ReverseEndianness(value.Reserved1),
            Reserved2 = BinaryPrimitives.ReverseEndianness(value.Reserved2),
        };
    }

    /// <summary>One <c>FeatureName</c> wire record: the feature type, its setting count, the offset of its settings, the feature flags, and the name table index for its label. Blittable, size 12.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The record describes a feature and points at its setting name array by offset from the start of the table. The semantic <see cref="FeatureName"/> replaces the offset with the resolved settings array.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">feature name table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FeatTable"/>
    /// <seealso cref="FeatureName"/>
    /// <seealso cref="SettingNameRecord"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">TrueType Reference Manual: The <c>feat</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct FeatureNameRecord : IEndianReversibleStruct<FeatureNameRecord>
    {
        /// <summary>The feature type at byte offset 0.</summary>
        /// <value>A value from the Apple Font Feature Registry. See <see cref="FeatTable.LanguageFeature"/> for the reserved type 39.</value>
        public ushort Feature;              // +0

        /// <summary>The number of setting name records at byte offset 2.</summary>
        public ushort SettingCount;         // +2

        /// <summary>The byte offset from the start of the table to this feature's setting name array at byte offset 4.</summary>
        public uint SettingTableOffset;     // +4

        /// <summary>The feature flag word at byte offset 8.</summary>
        public ushort FeatureFlags;         // +8

        /// <summary>The <c>name</c> table index for the feature's label at byte offset 10.</summary>
        /// <value>A signed 16-bit value. The specification requires the value to be greater than 255 and less than 32768.</value>
        public short NameIndex;             // +10

        /// <summary>Reverses the byte order of every field in a <see cref="FeatureNameRecord"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new record with each field reversed.</returns>
        public static FeatureNameRecord ReverseEndianness(FeatureNameRecord value) => new()
        {
            Feature = BinaryPrimitives.ReverseEndianness(value.Feature),
            SettingCount = BinaryPrimitives.ReverseEndianness(value.SettingCount),
            SettingTableOffset = BinaryPrimitives.ReverseEndianness(value.SettingTableOffset),
            FeatureFlags = BinaryPrimitives.ReverseEndianness(value.FeatureFlags),
            NameIndex = BinaryPrimitives.ReverseEndianness(value.NameIndex),
        };
    }

    /// <summary>One <c>SettingName</c> wire record: a setting value and the <c>name</c> table index for its label. Blittable, size 4.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>For non-exclusive features, only the on setting of each on/off pair appears in the array. The off setting is implicitly the on setting plus one.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">feature name table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FeatTable"/>
    /// <seealso cref="SettingName"/>
    /// <seealso cref="FeatureNameRecord"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">TrueType Reference Manual: The <c>feat</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct SettingNameRecord : IEndianReversibleStruct<SettingNameRecord>
    {
        /// <summary>The setting value at byte offset 0.</summary>
        public ushort Setting;    // +0

        /// <summary>The <c>name</c> table index for the setting's label at byte offset 2.</summary>
        /// <value>A signed 16-bit value. The specification requires the value to be greater than 255 and less than 32768.</value>
        public short NameIndex;   // +2

        /// <summary>Reverses the byte order of both fields in a <see cref="SettingNameRecord"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new record with each field reversed.</returns>
        public static SettingNameRecord ReverseEndianness(SettingNameRecord value) => new()
        {
            Setting = BinaryPrimitives.ReverseEndianness(value.Setting),
            NameIndex = BinaryPrimitives.ReverseEndianness(value.NameIndex),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the first byte of the <c>feat</c> table.</param>
    /// <param name="context">Unused. The table is self-contained.</param>
    /// <returns>The parsed <c>feat</c> table with every feature's settings resolved.</returns>
    /// <exception cref="InvalidDataException">The version is not 1.0, or a setting table offset exceeds the table-scoped source length.</exception>
    /// <exception cref="EndOfStreamException">The header, the feature name array, or a setting name array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The parse is eager: the feature name array is read in one bulk call, then each feature's setting name array is read by its declared offset.</description></item>
    /// <item><description>The setting name arrays can appear in any order in the file, and their offsets are measured from the start of the table. The parser therefore reads each array from a fresh cursor rather than sequentially.</description></item>
    /// <item><description>The specification requires the feature name array to be sorted by feature type; the parser does not enforce the ordering.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="FeatureNameRecord"/>
    /// <seealso cref="SettingNameRecord"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">TrueType Reference Manual: The <c>feat</c> table</seealso>
    static FeatTable IRecord<FeatTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version != CurrentVersion)
        {
            throw new InvalidDataException(
                $"'feat'.version is 0x{header.Version.Bits:X8}, expected 0x{CurrentVersion.Bits:X8}.");
        }

        long tableLength = cursor.Source.Length;

        FeatureNameRecord[] records = cursor.ReadEndianReversibleStructArray<FeatureNameRecord>(header.FeatureNameCount);

        var features = new FeatureName[records.Length];
        for (int i = 0; i < records.Length; i++)
        {
            FeatureNameRecord record = records[i];

            if (record.SettingTableOffset > tableLength)
            {
                throw new InvalidDataException(
                    $"'feat' feature {record.Feature} declares a setting table offset of {record.SettingTableOffset}, exceeding the table length {tableLength}.");
            }

            SettingNameRecord[] settingRecords = cursor.Source.ReadEndianReversibleStructArrayAt<SettingNameRecord>(record.SettingTableOffset, record.SettingCount);

            var settings = new SettingName[settingRecords.Length];
            for (int j = 0; j < settingRecords.Length; j++)
            {
                settings[j] = new SettingName
                {
                    Setting = settingRecords[j].Setting,
                    NameIndex = (ushort)settingRecords[j].NameIndex,
                };
            }

            features[i] = new FeatureName
            {
                Feature = record.Feature,
                Flags = (FeatureFlags)record.FeatureFlags,
                NameIndex = (ushort)record.NameIndex,
                Settings = settings,
            };
        }

        return new()
        {
            Version = header.Version,
            Features = features,
        };
    }
}

/// <summary>A parsed <c>feat</c> feature: the feature type, its flags, the <c>name</c> table index for its label, and the resolved setting array.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record replaces the wire <c>settingTable</c> offset with the resolved settings array; the offset is consumed during parse and is not exposed.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">feature name table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FeatTable"/>
/// <seealso cref="SettingName"/>
/// <seealso cref="FeatureFlags"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">TrueType Reference Manual: The <c>feat</c> table</seealso>
public sealed record FeatureName
{
    /// <summary>Gets the feature type.</summary>
    /// <value>A value from the Apple Font Feature Registry. See <see cref="FeatTable.LanguageFeature"/> for the reserved type 39.</value>
    /// <seealso cref="FeatTable.LanguageFeature"/>
    /// <seealso cref="Settings"/>
    public required ushort Feature { get; init; }

    /// <summary>Gets the feature flags.</summary>
    /// <value>A combination of <see cref="FeatureFlags"/> bits.</value>
    /// <seealso cref="FeatureFlags"/>
    /// <seealso cref="IsExclusive"/>
    /// <seealso cref="HasExplicitDefaultSetting"/>
    /// <seealso cref="DefaultSettingIndex"/>
    public required FeatureFlags Flags { get; init; }

    /// <summary>Gets the <c>name</c> table index for the feature's label.</summary>
    /// <value>A value greater than 255 and less than 32768, per the specification.</value>
    /// <seealso cref="SettingName.NameIndex"/>
    public required ushort NameIndex { get; init; }

    /// <summary>Gets the feature's settings, in the on-disk order.</summary>
    /// <value>The array of <see cref="SettingName"/> entries, sorted by <see cref="SettingName.Setting"/> per the specification. The parser does not enforce the ordering.</value>
    /// <remarks>For non-exclusive features, only the on setting of each on/off pair appears in the array; the off setting is implicitly the on setting plus one.</remarks>
    /// <seealso cref="SettingName"/>
    public required IReadOnlyList<SettingName> Settings { get; init; }

    /// <summary>Gets the number of settings.</summary>
    /// <value>The length of <see cref="Settings"/>.</value>
    /// <seealso cref="Settings"/>
    public int SettingCount => Settings.Count;

    /// <summary>Gets a value indicating whether the feature's settings are mutually exclusive.</summary>
    /// <value><see langword="true"/> when <see cref="FeatureFlags.Exclusive"/> is set.</value>
    /// <remarks>An exclusive feature has one setting active at a time. A non-exclusive feature has independent on/off settings, stored as pairs in <see cref="Settings"/>.</remarks>
    /// <seealso cref="FeatureFlags.Exclusive"/>
    public bool IsExclusive => (Flags & FeatureFlags.Exclusive) != 0;

    /// <summary>Gets a value indicating whether the flags declare an explicit default setting index.</summary>
    /// <value><see langword="true"/> when <see cref="FeatureFlags.ExplicitDefaultSetting"/> is set.</value>
    /// <remarks>When the flag is clear and the feature is exclusive, the default is the first setting in <see cref="Settings"/>.</remarks>
    /// <seealso cref="FeatureFlags.ExplicitDefaultSetting"/>
    /// <seealso cref="DefaultSettingIndex"/>
    public bool HasExplicitDefaultSetting => (Flags & FeatureFlags.ExplicitDefaultSetting) != 0;

    /// <summary>Gets the index of the setting that represents the feature's default behavior.</summary>
    /// <value>
    /// <list type="bullet">
    /// <item><description>When the feature is not exclusive: <c>0</c>. The flag bits that carry a default index are ignored for non-exclusive features.</description></item>
    /// <item><description>When the feature is exclusive and <see cref="HasExplicitDefaultSetting"/> is set: the value of <see cref="FeatTable.DefaultSettingIndexMask"/> bits of <see cref="Flags"/>.</description></item>
    /// <item><description>When the feature is exclusive and the explicit flag is clear: <c>0</c>, meaning the first setting in <see cref="Settings"/> is the default.</description></item>
    /// </list>
    /// </value>
    /// <seealso cref="HasExplicitDefaultSetting"/>
    /// <seealso cref="IsExclusive"/>
    /// <seealso cref="FeatTable.DefaultSettingIndexMask"/>
    public int DefaultSettingIndex => IsExclusive && HasExplicitDefaultSetting ? (int)Flags & FeatTable.DefaultSettingIndexMask : 0;
}

/// <summary>A parsed <c>feat</c> setting: a setting value and the <c>name</c> table index for its label.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">feature name table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FeatTable"/>
/// <seealso cref="FeatureName"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">TrueType Reference Manual: The <c>feat</c> table</seealso>
public sealed record SettingName
{
    /// <summary>Gets the setting value.</summary>
    /// <value>
    /// <list type="bullet">
    /// <item><description>For exclusive features, an arbitrary code that identifies the setting within its feature.</description></item>
    /// <item><description>For non-exclusive features, the on value of an on/off pair. The off value is this value plus one.</description></item>
    /// <item><description>For feature 39 (<see cref="FeatTable.LanguageFeature"/>), one plus an index into the <c>ltag</c> table; the value <c>0</c> means language-independent.</description></item>
    /// </list>
    /// </value>
    /// <seealso cref="NameIndex"/>
    /// <seealso cref="FeatTable.LanguageFeature"/>
    public required ushort Setting { get; init; }

    /// <summary>Gets the <c>name</c> table index for the setting's label.</summary>
    /// <value>A value greater than 255 and less than 32768, per the specification.</value>
    /// <seealso cref="FeatureName.NameIndex"/>
    public required ushort NameIndex { get; init; }
}

/// <summary>The flag bits stored in a <c>feat</c> feature's <c>featureFlags</c> field.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Only two bits are defined: the exclusivity flag at <c>0x8000</c> and the explicit-default flag at <c>0x4000</c>. The remaining bits are either reserved or hold the default setting index.</description></item>
/// <item><description>The low eight bits hold the default setting index and are only meaningful when <see cref="ExplicitDefaultSetting"/> is set and <see cref="Exclusive"/> is set. See <see cref="FeatureName.DefaultSettingIndex"/>.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">feature name table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FeatureName.Flags"/>
/// <seealso cref="FeatureName.DefaultSettingIndex"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">TrueType Reference Manual: The <c>feat</c> table</seealso>
[Flags]
public enum FeatureFlags : ushort
{
    /// <summary>No flags are set.</summary>
    /// <value>The constant <c>0</c>.</value>
    None = 0,

    /// <summary>Bit 15: the feature's settings are mutually exclusive.</summary>
    /// <value>The mask <c>0x8000</c>.</value>
    /// <remarks>When set, exactly one setting of the feature is active at a time. When clear, each setting is independently on or off.</remarks>
    /// <seealso cref="FeatureName.IsExclusive"/>
    Exclusive = 0x8000,

    /// <summary>Bit 14: the default setting index is carried in the low bits of <see cref="FeatureName.Flags"/>.</summary>
    /// <value>The mask <c>0x4000</c>.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>When the flag is set and the feature is exclusive, the default is the setting whose index is given by the low eight bits of <see cref="FeatureName.Flags"/>.</description></item>
    /// <item><description>When the flag is clear and the feature is exclusive, the default is the first setting in the array.</description></item>
    /// <item><description>The flag is ignored for non-exclusive features.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FeatureName.HasExplicitDefaultSetting"/>
    /// <seealso cref="FeatureName.DefaultSettingIndex"/>
    ExplicitDefaultSetting = 0x4000,
}
