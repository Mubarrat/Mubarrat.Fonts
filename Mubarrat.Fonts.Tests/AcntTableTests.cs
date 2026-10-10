using Mubarrat.Fonts.Tables;

namespace Mubarrat.Fonts.Tests;

/// <summary>
/// Tests for the <c>acnt</c> (accent attachment) table. No font in the test corpus
/// contains one, so each case is built as a synthetic table.
/// </summary>
public class AcntTableTests
{
    private const uint CurrentVersion = 0x00010000;

    // ─────────────────────── Construction helpers ───────────────────────

    /// <summary>Builds an <c>acnt</c> table with no descriptions and no secondary data.</summary>
    private static byte[] EmptyTable(ushort first = 0, ushort last = 0)
    {
        var bytes = new byte[AcntTable.HeaderSize];
        WriteUInt32(bytes, 0, CurrentVersion);
        WriteUInt16(bytes, 4, first);
        WriteUInt16(bytes, 6, last);
        WriteUInt32(bytes, 8, AcntTable.HeaderSize);
        WriteUInt32(bytes, 12, AcntTable.HeaderSize);
        WriteUInt32(bytes, 16, AcntTable.HeaderSize);
        return bytes;
    }

    /// <summary>Builds a format 0 description record: a packed flag and primary index, then the attachment point and secondary index.</summary>
    private static byte[] Format0Description(ushort primaryGlyphIndex, byte primaryAttachmentPoint, byte secondaryInfoIndex) =>
    [
        (byte)(primaryGlyphIndex >> 8),
        (byte)(primaryGlyphIndex & 0xFF),
        primaryAttachmentPoint,
        secondaryInfoIndex,
    ];

    /// <summary>Builds a format 1 description record: a packed flag and primary index, then the extension offset.</summary>
    private static byte[] Format1Description(ushort primaryGlyphIndex, ushort extensionOffset) =>
    [
        (byte)(0x80 | (primaryGlyphIndex >> 8)),
        (byte)(primaryGlyphIndex & 0xFF),
        (byte)(extensionOffset >> 8),
        (byte)(extensionOffset & 0xFF),
    ];

    private static byte[] SecondaryGlyph(ushort glyphIndex, byte attachmentNumber) =>
    [
        (byte)(glyphIndex >> 8),
        (byte)(glyphIndex & 0xFF),
        attachmentNumber,
    ];

    /// <summary>Packs one extension bit stream as a most-significant-bit-first sequence, mirroring how the parser reads it.</summary>
    private static byte[] BitWriter(Action<Action<int, int>> write)
    {
        var bits = new List<int>();

        // Each field is emitted most significant bit first, matching the parser's reading order.
        write((value, count) =>
        {
            for (int i = count - 1; i >= 0; i--)
                bits.Add((value >> i) & 1);
        });

        var bytes = new byte[(bits.Count + 7) / 8];
        for (int i = 0; i < bits.Count; i++)
            if (bits[i] != 0)
                bytes[i / 8] |= (byte)(0x80 >> (i % 8));

        return bytes;
    }

    private static void WriteUInt16(byte[] bytes, int offset, ushort value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)(value & 0xFF);
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    // ─────────────────────── Test data ───────────────────────

    public static TheoryData<string, uint[], string> NonCurrentVersions => new()
    {
        { "version 2.0", [0x00020000], "2.0" },
        { "version 0.0", [0x00000000], "0.0" },
        { "version 1.1", [0x00011000], "1.1" },
    };

    // ─────────────────────── Format 0 ───────────────────────

    [Fact]
    public void ParsesSingleAccentDescriptions()
    {
        // Two accented glyphs, 0x0100 and 0x0101, each with one accent.
        byte[] bytes =
        [
            .. EmptyTable(0x0100, 0x0101),
            .. Format0Description(50, 7, 0),
            .. Format0Description(51, 9, 1),
            .. SecondaryGlyph(500, 3),
            .. SecondaryGlyph(501, 4),
        ];

        var acnt = Parse.Table<AcntTable>(bytes);

        Assert.Equal("acnt", AcntTable.Tag.ToString());
        Assert.Equal(1, acnt.MajorVersion);
        Assert.Equal(0, acnt.MinorVersion);
        Assert.Equal(0x0100, acnt.FirstAccentGlyphIndex);
        Assert.Equal(0x0101, acnt.LastAccentGlyphIndex);
        Assert.Equal(2, acnt.GlyphCount);
        Assert.Equal(0, acnt.ExtensionCount);
        Assert.Empty(acnt.ExtensionData);

        Assert.Equal(50, acnt.Glyphs[0].PrimaryGlyphIndex);
        Assert.Equal(0, acnt.Glyphs[0].Format);
        Assert.Equal(7, acnt.Glyphs[0].PrimaryAttachmentPoint);
        Assert.Equal(0, acnt.Glyphs[0].SecondaryInfoIndex);
        Assert.Equal(-1, acnt.Glyphs[0].ExtensionIndex);

        Assert.Equal(51, acnt.Glyphs[1].PrimaryGlyphIndex);
        Assert.Equal(9, acnt.Glyphs[1].PrimaryAttachmentPoint);
        Assert.Equal(1, acnt.Glyphs[1].SecondaryInfoIndex);

        Assert.Equal(0x7F32, acnt.Glyphs[0].Raw);

        Assert.Equal(2, acnt.SecondaryGlyphs.Count);
        Assert.Equal(500, acnt.SecondaryGlyphs[0].SecondaryGlyphIndex);
        Assert.Equal(3, acnt.SecondaryGlyphs[0].SecondaryGlyphAttachmentNumber);
        Assert.Equal(501, acnt.SecondaryGlyphs[1].SecondaryGlyphIndex);
        Assert.Equal(4, acnt.SecondaryGlyphs[1].SecondaryGlyphAttachmentNumber);
    }

    [Fact]
    public void LooksUpDescriptionsAndAccentsByGlyphId()
    {
        byte[] bytes =
        [
            .. EmptyTable(0x0100, 0x0101),
            .. Format0Description(50, 7, 1),
            .. Format0Description(51, 9, 0),
            .. SecondaryGlyph(500, 3),
            .. SecondaryGlyph(501, 4),
        ];

        var acnt = Parse.Table<AcntTable>(bytes);

        Assert.Null(acnt.GetDescription(0x00FF));
        Assert.Null(acnt.GetDescription(0x0102));

        var description = acnt.GetDescription(0x0101);
        Assert.NotNull(description);
        Assert.Equal(51, description!.Value.PrimaryGlyphIndex);

        // A format 0 glyph exposes its single accent as a one-element list.
        var accents = acnt.GetAccents(0x0100);
        Assert.Single(accents);
        Assert.Equal(1, accents[0].SecondaryInfoIndex);
        Assert.Equal(7, accents[0].PrimaryAttachmentPoint);

        Assert.Empty(acnt.GetAccents(0x00FF));

        // The secondary record the accent names is the one with glyph 501.
        var secondary = acnt.SecondaryGlyphs[accents[0].SecondaryInfoIndex];
        Assert.Equal(501, secondary.SecondaryGlyphIndex);
        Assert.Equal(4, secondary.SecondaryGlyphAttachmentNumber);
    }

    // ─────────────────────── Format 1 ───────────────────────

    [Fact]
    public void ParsesPackedExtensionDataForMultipleAccents()
    {
        const ushort extensionOffset = 28;

        // The accents of glyph 0x0100: indices 1, 2, 3 and attachment points 10, 11, 12.
        byte[] extension = BitWriter(write =>
        {
            write(0, 1); write(1, 7); write(10, 8);
            write(0, 1); write(2, 7); write(11, 8);
            write(1, 1); write(3, 7); write(12, 8);
        });

        byte[] bytes =
        [
            .. EmptyTable(0x0100, 0x0100),
            .. Format1Description(60, 0),
            .. extension,                       // six bytes of packed accents, at table offset 28
            .. SecondaryGlyph(500, 3),
            .. SecondaryGlyph(501, 4),
            .. SecondaryGlyph(502, 5),
            .. SecondaryGlyph(503, 6),
        ];

        var acnt = Parse.Table<AcntTable>(bytes);

        Assert.Equal(extensionOffset, acnt.ExtensionOffset);
        Assert.Equal(1, acnt.GlyphCount);
        Assert.Equal(1, acnt.Glyphs[0].Format);
        Assert.Equal(60, acnt.Glyphs[0].PrimaryGlyphIndex);
        Assert.Equal(0, acnt.Glyphs[0].ExtensionOffset);
        Assert.Equal(0, acnt.Glyphs[0].ExtensionIndex);

        Assert.Single(acnt.ExtensionData);
        Assert.Equal(3, acnt.ExtensionData[0].Count);
        Assert.Equal(3, acnt.ExtensionCount);

        Assert.Equal(1, acnt.ExtensionData[0][0].SecondaryInfoIndex);
        Assert.Equal(10, acnt.ExtensionData[0][0].PrimaryAttachmentPoint);
        Assert.Equal(2, acnt.ExtensionData[0][1].SecondaryInfoIndex);
        Assert.Equal(11, acnt.ExtensionData[0][1].PrimaryAttachmentPoint);
        Assert.Equal(3, acnt.ExtensionData[0][2].SecondaryInfoIndex);
        Assert.Equal(12, acnt.ExtensionData[0][2].PrimaryAttachmentPoint);

        Assert.Equal(3, acnt.GetAccents(0x0100).Count);

        // Four secondary records: the table's own stride decides the count.
        Assert.Equal(4, acnt.SecondaryGlyphs.Count);
    }

    [Fact]
    public void ParsesExtensionDataThatSharesOneSecondaryRecord()
    {
        // A fit-like accent: the same accent index used twice on the same glyph.
        byte[] extension = BitWriter(write =>
        {
            write(0, 1); write(0, 7); write(5, 8);
            write(1, 1); write(0, 7); write(9, 8);
        });

        byte[] bytes =
        [
            .. EmptyTable(0x0100, 0x0100),
            .. Format1Description(70, 0),
            .. extension,
            .. SecondaryGlyph(600, 1),
        ];

        var acnt = Parse.Table<AcntTable>(bytes);

        Assert.Equal(2, acnt.ExtensionData[0].Count);
        Assert.Equal(acnt.SecondaryGlyphs[0].SecondaryGlyphIndex, 600);
        Assert.All(acnt.ExtensionData[0], static c => Assert.Equal(0, c.SecondaryInfoIndex));
        Assert.Equal(5, acnt.ExtensionData[0][0].PrimaryAttachmentPoint);
        Assert.Equal(9, acnt.ExtensionData[0][1].PrimaryAttachmentPoint);
    }

    [Fact]
    public void ParsesMultipleFormat1Descriptions()
    {
        const ushort extensionOffset = 28;

        byte[] first = BitWriter(write =>
        {
            write(0, 1); write(1, 7); write(10, 8);
            write(1, 1); write(2, 7); write(11, 8);
        });
        byte[] second = BitWriter(write =>
        {
            write(1, 1); write(3, 7); write(12, 8);
        });

        byte[] bytes =
        [
            .. EmptyTable(0x0100, 0x0101),
            .. Format1Description(60, 0),
            .. Format1Description(61, (ushort)first.Length),
            .. first,
            .. second,
            .. SecondaryGlyph(500, 3),
            .. SecondaryGlyph(501, 4),
            .. SecondaryGlyph(502, 5),
        ];

        var acnt = Parse.Table<AcntTable>(bytes);

        Assert.Equal(extensionOffset, acnt.ExtensionOffset);
        Assert.Equal(2, acnt.GlyphCount);

        // Each format 1 description gets its own entry in the extension list, in description order.
        Assert.Equal(0, acnt.Glyphs[0].ExtensionIndex);
        Assert.Equal(1, acnt.Glyphs[1].ExtensionIndex);

        Assert.Equal(2, acnt.ExtensionData[0].Count);
        Assert.Single(acnt.ExtensionData[1]);
        Assert.Equal(3, acnt.ExtensionCount);

        Assert.Equal(10, acnt.ExtensionData[0][0].PrimaryAttachmentPoint);
        Assert.Equal(11, acnt.ExtensionData[0][1].PrimaryAttachmentPoint);
        Assert.Equal(12, acnt.ExtensionData[1][0].PrimaryAttachmentPoint);

        // The per-glyph lookup reaches the right list for each glyph.
        Assert.Equal(2, acnt.GetAccents(0x0100).Count);
        Assert.Single(acnt.GetAccents(0x0101));
        Assert.Equal(12, acnt.GetAccents(0x0101)[0].PrimaryAttachmentPoint);
        Assert.Equal(3, acnt.GetAccents(0x0101)[0].SecondaryInfoIndex);
    }

    // ─────────────────────── Format mixing ───────────────────────

    [Fact]
    public void RejectsMixedDescriptionFormats()
    {
        byte[] bytes =
        [
            .. EmptyTable(0x0100, 0x0101),
            .. Format0Description(50, 7, 0),
            .. Format1Description(51, 0),
            .. SecondaryGlyph(500, 3),
        ];

        var exception = Assert.Throws<InvalidDataException>(() => Parse.Table<AcntTable>(bytes));
        Assert.Contains("mixed-format", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ─────────────────────── Edge cases ───────────────────────

    [Fact]
    public void EmptyAccentRangeYieldsNoDescriptions()
    {
        var acnt = Parse.Table<AcntTable>(EmptyTable(0x0200, 0x0100));

        Assert.Equal(0, acnt.GlyphCount);
        Assert.Empty(acnt.Glyphs);
        Assert.Empty(acnt.ExtensionData);
        Assert.Empty(acnt.SecondaryGlyphs);
        Assert.Null(acnt.GetDescription(0x0180));
        Assert.Empty(acnt.GetAccents(0x0180));
    }

    [Fact]
    public void SecondaryDataIsCappedAtTheSevenBitIndexLimit()
    {
        // A secondary region longer than 255 records must be truncated to what an index can address.
        byte[] records = [.. Enumerable.Range(0, 300).Select(i => SecondaryGlyph((ushort)i, (byte)(i & 0xFF))).SelectMany(static r => r)];
        byte[] bytes = [.. EmptyTable(0, 0), .. records];

        var acnt = Parse.Table<AcntTable>(bytes);

        Assert.Equal(AcntTable.MaxSecondaryGlyphCount, acnt.SecondaryGlyphs.Count);
        Assert.Equal(0, acnt.SecondaryGlyphs[0].SecondaryGlyphIndex);
        Assert.Equal(254, acnt.SecondaryGlyphs[^1].SecondaryGlyphIndex);
    }

    [Fact]
    public void EnforcesTheSecondaryRecordStride()
    {
        // 9 bytes of secondary data is exactly 3 records with no remainder.
        byte[] bytes =
        [
            .. EmptyTable(0, 0),
            .. SecondaryGlyph(1, 1),
            .. SecondaryGlyph(2, 2),
            .. SecondaryGlyph(3, 3),
        ];

        var acnt = Parse.Table<AcntTable>(bytes);

        Assert.Equal(3, acnt.SecondaryGlyphs.Count);
    }

    [Theory]
    [MemberData(nameof(NonCurrentVersions))]
    public void ParsesNonCurrentVersionsWithoutRejectingThem(string _, uint[] version, string expected)
    {
        // The table is discouraged and no longer supported, so an unexpected version is
        // preserved for inspection rather than treated as a parse failure.
        byte[] bytes = EmptyTable();
        WriteUInt32(bytes, 0, version[0]);

        var acnt = Parse.Table<AcntTable>(bytes);

        Assert.Equal(expected, $"{acnt.MajorVersion}.{acnt.MinorVersion}");
    }

    [Fact]
    public void ExposesTheHeaderOffsets()
    {
        byte[] bytes = EmptyTable();
        WriteUInt32(bytes, 8, 24);
        WriteUInt32(bytes, 12, 40);
        WriteUInt32(bytes, 16, 64);

        var acnt = Parse.Table<AcntTable>(bytes);

        Assert.Equal(24u, acnt.DescriptionOffset);
        Assert.Equal(40u, acnt.ExtensionOffset);
        Assert.Equal(64u, acnt.SecondaryOffset);
    }

    // ─────────────────────── Record structs ───────────────────────

    [Fact]
    public void GlyphDescriptionNormalizesThePackedFormatFlag()
    {
        // Format 0, primary glyph index 0x7F32.
        byte[] format0 = Format0Description(0x7F32, 7, 9);
        var description = Parse.Table<AcntTable>([.. EmptyTable(0x0100, 0x0100), .. format0]).Glyphs[0];

        Assert.Equal(0, description.Format);
        Assert.Equal(0x7F32, description.PrimaryGlyphIndex);
        Assert.Equal(0x7F32, description.Raw);
        Assert.Equal(7, description.PrimaryAttachmentPoint);
        Assert.Equal(9, description.SecondaryInfoIndex);
    }

    [Fact]
    public void HeaderSizeAndRecordSizesMatchTheOnDiskLayout()
    {
        Assert.Equal(20, AcntTable.HeaderSize);
        Assert.Equal(4, AcntTable.DescriptionSize);
        Assert.Equal(3, AcntTable.SecondaryGlyphSize);
        Assert.Equal(0, AcntTable.DescriptionFormatSingle);
        Assert.Equal(1, AcntTable.DescriptionFormatExtended);
        Assert.Equal(255, AcntTable.MaxSecondaryGlyphCount);
    }
}
