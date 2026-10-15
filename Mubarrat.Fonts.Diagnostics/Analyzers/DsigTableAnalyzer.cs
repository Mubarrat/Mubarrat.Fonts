using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>DSIG</c> table.</summary>
/// <remarks>
/// <para>
/// The parser already enforces the version discriminant and bounds every signature block
/// against the table's declared extent. What remains for analysis is the shape of the
/// signature records themselves: format discriminant, block-length consistency, and the
/// reserved bits in the flags field.
/// </para>
/// <para>
/// Signature verification — the actual PKCS#7 cryptography — is out of scope. The rules
/// here check structure, not validity.
/// </para>
/// </remarks>
public class DsigTableAnalyzer : IFontAnalyzer
{
    /// <summary>Size of the format-1 block header: reserved1 (2), reserved2 (2), signatureLength (4).</summary>
    private const uint Format1BlockHeaderSize = 8;

    public static readonly DiagnosticDescriptor SignatureFormatUnknown = new(
        "OT.dsig.signature-format", "Unrecognized signature format",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Signature {0} has format {1}; only format 1 is defined.",
        "The specification defines only format 1. Other values indicate either a future extension or a malformed record.");

    public static readonly DiagnosticDescriptor SignatureBlockLengthMismatch = new(
        "OT.dsig.block-length", "Signature block length does not match the format's layout",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "Signature {0} declares length {1} but the format-1 layout requires {2} bytes.",
        "A format-1 block consists of an 8-byte header followed by the signature payload. The outer Length field must equal 8 plus the signature length.");

    public static readonly DiagnosticDescriptor EmptySignature = new(
        "OT.dsig.empty-signature", "Signature payload is empty",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "Signature {0} has a zero-length payload.",
        "Every signature must carry a PKCS#7 packet. A zero-length payload means the record was written without content.");

    public static readonly DiagnosticDescriptor FlagsReservedBits = new(
        "OT.dsig.flags-reserved", "Reserved DSIG flag bits set",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "flags is 0x{0:X4}; bits 1–15 are reserved and must be zero.",
        "Only bit 0 of the flags field is defined: it prevents a font from being re-signed.");

    public static readonly DiagnosticDescriptor NoSignatures = new(
        "OT.dsig.no-signatures", "DSIG has no signatures",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "The table declares {0} signature records.",
        "A DSIG table with zero signatures provides no authenticity information and could be omitted.");

    public static readonly DiagnosticDescriptor CannotBeResignedDeclared = new(
        "OT.dsig.cannot-be-resigned", "Font declares that it cannot be re-signed",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "The CannotBeResigned flag is set.",
        "Tools that modify the font must remove the DSIG table rather than re-sign it. Informational; the flag is a deliberate declaration by the font vendor.");

    public static readonly DiagnosticDescriptor MultipleSignatures = new(
        "OT.dsig.multiple-signatures", "Font carries multiple signatures",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "The table declares {0} signatures.",
        "Multiple signatures are legal. Consumers that validate only the first may miss a second signature from a different authority.");

    private DsigTableAnalyzer() { }

    public static DsigTableAnalyzer Instance => field ??= new();

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var dsig = face.GetTable<DsigTable>();
        var tag = DsigTable.Tag;

        AnalyzeHeader(dsig, tag, bag);
        AnalyzeSignatures(dsig, bag);
    }

    private static void AnalyzeHeader(DsigTable dsig, Tag tag, DiagnosticBag bag)
    {
        ushort reservedFlags = (ushort)(dsig.Flags & 0xFFFE);
        if (reservedFlags != 0)
        {
            bag.Add(FlagsReservedBits.Create(
                [dsig.Flags],
                table: tag, field: nameof(DsigTable.Flags)));
        }

        if (dsig.Count == 0)
        {
            bag.Add(NoSignatures.Create(
                [dsig.Count],
                table: tag, field: nameof(DsigTable.Signatures)));
        }
        else if (dsig.Count > 1)
        {
            bag.Add(MultipleSignatures.Create(
                [dsig.Count],
                table: tag, field: nameof(DsigTable.Signatures)));
        }

        if (dsig.CannotBeResigned)
        {
            bag.Add(CannotBeResignedDeclared.Create(
                [],
                table: tag, field: nameof(DsigTable.Flags)));
        }
    }

    private static void AnalyzeSignatures(DsigTable dsig, DiagnosticBag bag)
    {
        for (int i = 0; i < dsig.Signatures.Count; i++)
        {
            DsigSignature sig = dsig.Signatures[i];

            if (sig.Format != 1)
            {
                bag.Add(SignatureFormatUnknown.Create(
                    [i, sig.Format],
                    table: DsigTable.Tag, field: $"signatures[{i}].Format"));
            }

            if (sig.SignatureLength == 0)
            {
                bag.Add(EmptySignature.Create(
                    [i],
                    table: DsigTable.Tag, field: $"signatures[{i}].Signature"));
            }

            if (sig.Format == 1)
            {
                uint expected = Format1BlockHeaderSize + (uint)sig.SignatureLength;
                if (sig.Length != expected)
                {
                    bag.Add(SignatureBlockLengthMismatch.Create(
                        [i, sig.Length, expected],
                        table: DsigTable.Tag, field: $"signatures[{i}].Length"));
                }
            }
        }
    }
}
