using System.Security.Cryptography;

namespace PincabToolbox.Repair.Licensing;

public interface ILicenseVerifier
{
    /// <summary>Never throws. A missing, malformed, tampered, or forged key is simply Invalid.</summary>
    LicenseCheckResult Verify(string? licenseKey);
}

/// <summary>
/// Verifies a license key against the embedded PUBLIC key only. 100% local, zero network call —
/// ADR-002 / ADR-009. ECDSA (P-256) rather than a shared secret: the key that ships inside the
/// App can only verify, never forge, a license — extracting it from the binary (trivial, for
/// anyone determined enough) gains an attacker nothing towards minting their own valid key.
/// Matches the project's "anti-piratage volontairement léger" stance (ADR-002) without leaving
/// the door fully open to trivial key generators.
///
/// The matching PRIVATE key never ships with the App. It is generated and held only by
/// <c>PincabToolbox.LicenseTool</c>, run offline on Maxime's own machine — see that tool's README.
/// </summary>
public sealed class LicenseVerifier : ILicenseVerifier
{
    /// <summary>
    /// ECDSA P-256 public key, X.509 SubjectPublicKeyInfo (DER), base64-encoded. Safe to embed —
    /// this is the PUBLIC half; it can verify signatures but cannot produce them. Generated once
    /// with `dotnet run --project tools/PincabToolbox.LicenseTool -- init`, printed to stdout by
    /// that command, and pasted here.
    ///
    /// Real key, generated 13/08/2026 by Maxime via `license-tool init` (offline, on his own
    /// machine — the matching private key never touched this repo or any session). From this
    /// commit on, only that private key can produce a license this build will accept; any license
    /// signed against a previous key (including any earlier real or placeholder value this
    /// constant may have held) stays invalid.
    /// </summary>
    public const string EmbeddedPublicKeyBase64 = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEJzN5IV+cxt+JTxae4VPjGbAnPJ5agHwUGonMKaukiRVX9Gx6n3s9bMreUamAPvrfu+bObWQtPScqUdJGtSrypQ==";

    private readonly ECDsa? _publicKey;
    private readonly string? _keyError;

    /// <summary>Uses the embedded public key — this is what the shipped App calls.</summary>
    public LicenseVerifier() : this(EmbeddedPublicKeyBase64) { }

    /// <summary>
    /// Verify against an explicit public key — used by tests and by the license tool itself.
    /// Never throws, even if <paramref name="publicKeyBase64"/> is garbage: a broken embedded key
    /// must never crash the shipped App on startup. Every <see cref="Verify"/> call simply returns
    /// Invalid until a real key is supplied.
    /// </summary>
    public LicenseVerifier(string publicKeyBase64)
    {
        // Revue sécurité 2026-08-05 (mandat "vérifie la protection licence") : ImportSubjectPublicKeyInfo
        // peut lever APRÈS que ECDsa.Create() ait déjà pris un handle crypto natif — vrai pour
        // EmbeddedPublicKeyBase64 tant qu'aucune vraie clé n'était encore embarquée (avant le
        // 13/08/2026), et reste vrai aujourd'hui pour toute clé invalide passée au constructeur
        // explicite (tests, LicenseTool). Sans le try/finally ci-dessous, chaque instanciation sur
        // une clé invalide fuyait ce handle.
        ECDsa? key = null;
        try
        {
            key = ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyBase64), out _);
            _publicKey = key;
        }
        catch (Exception e)
        {
            key?.Dispose();
            _publicKey = null;
            _keyError = $"license verification is not configured (invalid public key: {e.GetType().Name}) — " +
                         "run `license-tool init` and paste the real key into LicenseVerifier.EmbeddedPublicKeyBase64";
        }
    }

    /// <summary>
    /// Revue sécurité 2026-08-05 : borne défensive avant tout décodage. Une vraie licence fait
    /// quelques centaines de caractères (base64url(payload).base64url(signature)) ; ceci n'existe
    /// que pour rejeter vite un copier-coller massivement erroné sans décoder inutilement une
    /// chaîne de plusieurs Mo en mémoire. Généreux exprès pour ne jamais gêner un cas réel.
    /// </summary>
    private const int MaxLicenseKeyLength = 4096;

    public LicenseCheckResult Verify(string? licenseKey)
    {
        var publicKey = _publicKey;
        if (publicKey is null)
            return LicenseCheckResult.Invalid(_keyError!);

        if (string.IsNullOrWhiteSpace(licenseKey))
            return LicenseCheckResult.Invalid("empty license key");

        if (licenseKey.Length > MaxLicenseKeyLength)
            return LicenseCheckResult.Invalid("malformed license key (too long)");

        // Users copy-paste from an email — tolerate surrounding whitespace/newlines.
        var key = licenseKey.Trim();

        var parts = key.Split('.');
        if (parts.Length != 2)
            return LicenseCheckResult.Invalid("malformed license key (expected two parts)");

        var payloadBytes = LicenseCodec.Base64UrlDecode(parts[0]);
        var signatureBytes = LicenseCodec.Base64UrlDecode(parts[1]);
        if (payloadBytes is null || signatureBytes is null)
            return LicenseCheckResult.Invalid("malformed license key (not valid base64url)");

        bool signatureOk;
        try
        {
            signatureOk = publicKey.VerifyData(
                payloadBytes, signatureBytes, HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        catch
        {
            // A hand-edited or truncated signature can throw inside the crypto provider rather
            // than just returning false. Either way it is not a valid license — never let a
            // malformed key crash the caller.
            return LicenseCheckResult.Invalid("malformed signature");
        }

        if (!signatureOk)
            return LicenseCheckResult.Invalid("signature does not match — this key was altered or is not genuine");

        var payload = LicenseCodec.Deserialize(payloadBytes);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Email))
            return LicenseCheckResult.Invalid("malformed payload");

        return LicenseCheckResult.Valid(payload);
    }
}
