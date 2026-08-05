using System.Security.Cryptography;

namespace PincabToolbox.Repair.Licensing;

/// <summary>
/// Signs a license payload with the PRIVATE key. This class only ever runs offline, inside
/// <c>PincabToolbox.LicenseTool</c> (Maxime's own machine, at the moment of a sale) — the private
/// key never ships inside the App, and this class does not embed one. Shipping the signer in the
/// same assembly as the verifier is safe: signing without the private key is impossible, and the
/// algorithm itself is not a secret (Kerckhoffs's principle — only the key must stay hidden).
/// </summary>
public static class LicenseSigner
{
    public static string Sign(LicensePayload payload, ECDsa privateKey)
    {
        var payloadBytes = LicenseCodec.Serialize(payload);
        var signature = privateKey.SignData(
            payloadBytes, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        return LicenseCodec.Base64UrlEncode(payloadBytes) + "." + LicenseCodec.Base64UrlEncode(signature);
    }
}
