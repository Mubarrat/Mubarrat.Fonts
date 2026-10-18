using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

    /// <summary>Gets the feature name entries, sorted by <see cref="FeatureName.Feature"/>.</summary>
    /// <value>A read-only view over the array of <see cref="FeatureName"/> entries. The parser sorts the underlying array after reading it, so the sorted invariant that <see cref="TryGetFeature(ushort, out FeatureName?)"/> relies on is always satisfied, and the read-only surface prevents mutation after parse.</value>
    /// <remarks>Each entry carries its own settings array, resolved during parse from the entry's <c>settingTable</c> offset.</remarks>
    /// <seealso cref="FeatureName"/>
    /// <seealso cref="TryGetFeature(ushort, out FeatureName?)"/>
    public required IReadOnlyList<FeatureName> Features { get; init; }

    /// <summary>Gets the number of feature entries.</summary>
    /// <value>The count of <see cref="Features"/>.</value>
    /// <seealso cref="Features"/>
    public int FeatureCount => Features.Count;

    /// <summary>Tries to find a feature by feature type.</summary>
    /// <param name="featureType">The feature type to look up.</param>
    /// <param name="feature">When this method returns <see langword="true"/>, contains the feature; otherwise, <c>null</c>.</param>
    /// <returns><see langword="true"/> when a feature with the given type is present; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup is a binary search over <see cref="Features"/>, which the parser leaves sorted by feature type.</description></item>
    /// <item><description>When duplicate feature types are present — which the specification does not forbid — the entry at the binary-search hit position is returned; for a well-formed font this is the same entry a linear scan would find.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Features"/>
    /// <seealso cref="FeatureName"/>
    public bool TryGetFeature(ushort featureType, [NotNullWhen(true)] out FeatureName? feature)
    {
        int lo = 0;
        int hi = Features.Count - 1;

        while (lo <= hi)
        {
            int mid = (lo + hi) >>> 1;
            ushort current = Features[mid].Feature;

            if (current == featureType)
            {
                feature = Features[mid];
                return true;
            }

            if (current < featureType) lo = mid + 1;
            else hi = mid - 1;
        }

        feature = null;
        return false;
    }

    /// <summary>The 12-byte <c>feat</c> table header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by the feature name array, then by the setting name arrays, each at its own offset.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">feature name table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FeatTable"/>
    /// <seealso cref="FeatureName.Header"/>
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

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the first byte of the <c>feat</c> table.</param>
    /// <param name="context">Unused. The table is self-contained.</param>
    /// <returns>The parsed <c>feat</c> table with every feature's settings resolved and the feature array sorted by feature type.</returns>
    /// <exception cref="InvalidDataException">The version is not 1.0, or a setting table offset exceeds the table-scoped source length.</exception>
    /// <exception cref="EndOfStreamException">The header, the feature name array, or a setting name array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The parse is eager: the feature name array is read in one bulk call, then each feature's setting name array is read by its declared offset.</description></item>
    /// <item><description>The setting name arrays can appear in any order in the file, and their offsets are measured from the start of the table. The parser therefore reads each array from the source by offset rather than sequentially.</description></item>
    /// <item><description>The specification requires the feature name array to be sorted by feature type; the parser re-sorts the materialized array so the sorted invariant that <see cref="TryGetFeature(ushort, out FeatureName?)"/> relies on always holds, even for a font that shipped an unsorted array.</description></item>
    /// <item><description>The wire <see cref="SettingName"/> record is used directly as the semantic setting type, because the two are structurally identical: an <c>UInt16</c> setting value and an <c>UInt16</c> name table index.</description></item>
    /// <item><description>Each array is wrapped in <see cref="Array.AsReadOnly{T}(T[])"/> before being exposed, so callers cannot mutate it through the record's surface.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="FeatureName.Header"/>
    /// <seealso cref="SettingName"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">TrueType Reference Manual: The <c>feat</c> table</seealso>
    static FeatTable IRecord<FeatTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();
        if (header.Version != CurrentVersion)
            throw new InvalidDataException($"'feat'.version is 0x{header.Version.Bits:X8}, expected 0x{CurrentVersion.Bits:X8}.");
        var features = cursor.ReadEndianReversibleHeaderRecordArray<FeatureName, FeatureName.Header>(header.FeatureNameCount, new ParentContext(cursor.Source));
        Array.Sort(features, static (a, b) => a.Feature.CompareTo(b.Feature));
        return new()
        {
            Version = header.Version,
            Features = Array.AsReadOnly(features),
        };
    }
}

/// <summary>A parsed <c>feat</c> feature: the feature type, its flags, the <c>name</c> table index for its label, and the resolved setting array.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record replaces the wire <c>settingTable</c> offset with the resolved settings array; the offset is consumed during parse and is not exposed.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description><see cref="Settings"/> is typed <see cref="IReadOnlyList{T}"/> so the record's surface cannot mutate the underlying storage.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">feature name table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FeatTable"/>
/// <seealso cref="SettingName"/>
/// <seealso cref="FeatureFlags"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">TrueType Reference Manual: The <c>feat</c> table</seealso>
public sealed record FeatureName : IEndianReversibleHeaderRecord<FeatureName, FeatureName.Header>
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
    /// <value>A read-only view over the array of <see cref="SettingName"/> entries, sorted by <see cref="SettingName.Setting"/> per the specification. The parser does not enforce the ordering.</value>
    /// <remarks>For non-exclusive features, only the on setting of each on/off pair appears in the array; the off setting is implicitly the on setting plus one.</remarks>
    /// <seealso cref="SettingName"/>
    public required IReadOnlyList<SettingName> Settings { get; init; }

    /// <summary>Gets the number of settings.</summary>
    /// <value>The count of <see cref="Settings"/>.</value>
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

    static FeatureName IHeaderRecord<FeatureName, Header>.FromHeader(in Header header, object? context)
    {
        if (context is not ParentContext ctx)
            throw new InvalidDataException("'feat' feature requires a ParentContext carrying the table-scoped source.");

        long settingBytes = header.SettingTableOffset + (long)header.SettingCount * Unsafe.SizeOf<SettingName>();
        if (settingBytes > ctx.ParentSource.Length)
            throw new InvalidDataException(
                $"'feat' feature {header.Feature} declares {header.SettingCount} settings at offset {header.SettingTableOffset}, which exceeds the table length {ctx.ParentSource.Length}.");

        return new()
        {
            Feature = header.Feature,
            Flags = (FeatureFlags)header.FeatureFlags,
            NameIndex = header.NameIndex,
            Settings = Array.AsReadOnly(
                ctx.ParentSource.ReadEndianReversibleStructArrayAt<SettingName>(
                    header.SettingTableOffset, header.SettingCount)),
        };
    }

    /// <summary>One <c>FeatureName</c> wire record: the feature type, its setting count, the offset of its settings, the feature flags, and the name table index for its label. Blittable, size 12.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The record describes a feature and points at its setting name array by offset from the start of the table. The semantic <see cref="FeatureName"/> replaces the offset with the resolved settings array.</description></item>
    /// <item><description><see cref="NameIndex"/> is typed <c>UInt16</c> rather than <c>Int16</c>: the specification guarantees the value lies in <c>[256, 32767]</c>, so no sign extension is ever meaningful.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">feature name table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FeatTable"/>
    /// <seealso cref="FeatureName"/>
    /// <seealso cref="SettingName"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">TrueType Reference Manual: The <c>feat</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
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
        /// <value>The specification requires the value to be greater than 255 and less than 32768, which fits <c>UInt16</c> without sign extension.</value>
        public ushort NameIndex;            // +10

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new record with each field reversed.</returns>
        public static Header ReverseEndianness(Header value) => new()
        {
            Feature = BinaryPrimitives.ReverseEndianness(value.Feature),
            SettingCount = BinaryPrimitives.ReverseEndianness(value.SettingCount),
            SettingTableOffset = BinaryPrimitives.ReverseEndianness(value.SettingTableOffset),
            FeatureFlags = BinaryPrimitives.ReverseEndianness(value.FeatureFlags),
            NameIndex = BinaryPrimitives.ReverseEndianness(value.NameIndex),
        };
    }
}

/// <summary>A <c>feat</c> setting: a setting value and the <c>name</c> table index for its label.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Matches the on-disk <c>SettingName</c> record exactly: 4 bytes, big-endian, no padding. Because the wire and semantic shapes coincide, one type serves both roles rather than duplicating the two fields.</description></item>
/// <item><description>For exclusive features, <see cref="Setting"/> is an arbitrary code that identifies the setting within its feature. For non-exclusive features, it is the on value of an on/off pair, and the off value is one greater.</description></item>
/// <item><description>For feature 39 (<see cref="FeatTable.LanguageFeature"/>), <see cref="Setting"/> is one plus an index into the <c>ltag</c> table; the value <c>0</c> means language-independent.</description></item>
/// <item><description><see cref="NameIndex"/> is typed <c>UInt16</c> because the specification bounds it to <c>[256, 32767]</c>; no sign extension is meaningful.</description></item>
/// <item><description>Declared as a <c>record struct</c> so value equality, <c>ToString</c>, and the compiler-generated <c>&lt;Clone&gt;$()</c> come for free; the <c>&lt;Clone&gt;$()</c> method must appear in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">feature name table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FeatTable"/>
/// <seealso cref="FeatureName"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6feat.html">TrueType Reference Manual: The <c>feat</c> table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct SettingName : IEndianReversibleStruct<SettingName>
{
    /// <summary>The setting value at byte offset 0.</summary>
    /// <value>
    /// <list type="bullet">
    /// <item><description>For exclusive features, an arbitrary code that identifies the setting within its feature.</description></item>
    /// <item><description>For non-exclusive features, the on value of an on/off pair; the off value is this value plus one.</description></item>
    /// <item><description>For feature 39 (<see cref="FeatTable.LanguageFeature"/>), one plus an index into the <c>ltag</c> table; the value <c>0</c> means language-independent.</description></item>
    /// </list>
    /// </value>
    /// <seealso cref="NameIndex"/>
    /// <seealso cref="FeatTable.LanguageFeature"/>
    public ushort Setting;    // +0

    /// <summary>The <c>name</c> table index for the setting's label at byte offset 2.</summary>
    /// <value>A value greater than 255 and less than 32768, per the specification.</value>
    /// <seealso cref="FeatureName.NameIndex"/>
    public ushort NameIndex;  // +2

    /// <summary>Reverses the byte order of both fields in a <see cref="SettingName"/>.</summary>
    /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new setting with both fields reversed.</returns>
    public static SettingName ReverseEndianness(SettingName value) => new()
    {
        Setting = BinaryPrimitives.ReverseEndianness(value.Setting),
        NameIndex = BinaryPrimitives.ReverseEndianness(value.NameIndex),
    };
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
