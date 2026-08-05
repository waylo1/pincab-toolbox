using System.Security.Cryptography;
using PincabToolbox.Repair.Licensing;

namespace PincabToolbox.Repair.Tests;

/// <summary>
/// Uses freshly-generated test keypairs throughout — never the real embedded production key
/// (which does not exist yet at this point in the FIELD-LOG: Maxime has not run `license-tool
/// init` for real). Signature verification is deterministic given a fixed keypair and payload,
/// so a random test keypair is fine here: we are testing the ALGORITHM, not any specific key.
/// </summary>
public static class LicenseTests
{
    private static (ECDsa priv, string pubBase64) NewKeypair()
    {
        var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return (key, Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()));
    }

    private static LicensePayload SamplePayload() => new()
    {
        Email = "maxime@example.com",
        IssuedUtc = new DateTimeOffset(2026, 8, 4, 12, 0, 0, TimeSpan.Zero),
        UpdatesUntilUtc = new DateTimeOffset(2027, 8, 4, 12, 0, 0, TimeSpan.Zero),
    };

    public static void Test_ValidLicense_RoundTrips()
    {
        var (priv, pub) = NewKeypair();
        var key = LicenseSigner.Sign(SamplePayload(), priv);

        var result = new LicenseVerifier(pub).Verify(key);

        A.True(result.IsValid, "a freshly-signed license must verify");
        A.Equal("maxime@example.com", result.Payload!.Email, "email round-trips");
    }

    public static void Test_CopyPasteWhitespace_StillValid()
    {
        var (priv, pub) = NewKeypair();
        var key = LicenseSigner.Sign(SamplePayload(), priv);

        var result = new LicenseVerifier(pub).Verify("  \n" + key + "  \n");

        A.True(result.IsValid, "surrounding whitespace from a copy-paste must not break verification");
    }

    public static void Test_TamperedPayload_Invalid()
    {
        var (priv, pub) = NewKeypair();
        var key = LicenseSigner.Sign(SamplePayload(), priv);
        var parts = key.Split('.');

        // Flip one character in the payload half — still valid base64url shape, wrong content.
        var tamperedPayload = FlipOneChar(parts[0]);
        var tampered = tamperedPayload + "." + parts[1];

        var result = new LicenseVerifier(pub).Verify(tampered);

        A.False(result.IsValid, "a modified payload must fail signature verification");
    }

    public static void Test_TamperedSignature_Invalid()
    {
        var (priv, pub) = NewKeypair();
        var key = LicenseSigner.Sign(SamplePayload(), priv);
        var parts = key.Split('.');

        var tampered = parts[0] + "." + FlipOneChar(parts[1]);

        var result = new LicenseVerifier(pub).Verify(tampered);

        A.False(result.IsValid, "a modified signature must fail verification");
    }

    public static void Test_WrongPublicKey_Invalid()
    {
        var (priv, _) = NewKeypair();
        var (_, otherPub) = NewKeypair();   // a different keypair entirely — simulates a forged key
        var key = LicenseSigner.Sign(SamplePayload(), priv);

        var result = new LicenseVerifier(otherPub).Verify(key);

        A.False(result.IsValid, "a license signed by a different private key must not verify");
    }

    public static void Test_Garbage_NeverThrows()
    {
        var (_, pub) = NewKeypair();
        var verifier = new LicenseVerifier(pub);

        foreach (var garbage in new[] { "", "   ", "not-a-license", "a.b.c", "a.b", ".", "🤔.🤔", new string('x', 5000) })
        {
            var result = verifier.Verify(garbage);
            A.False(result.IsValid, $"garbage input '{garbage[..Math.Min(20, garbage.Length)]}' must be Invalid, not throw");
        }
    }

    public static void Test_NullOrEmpty_Invalid()
    {
        var (_, pub) = NewKeypair();
        var verifier = new LicenseVerifier(pub);

        A.False(verifier.Verify(null).IsValid, "null license key is invalid");
        A.False(verifier.Verify("").IsValid, "empty license key is invalid");
    }

    public static void Test_LicenseNeverExpires_UpdatesWindowIsSeparate()
    {
        var (priv, pub) = NewKeypair();
        var expiredUpdates = SamplePayload() with { UpdatesUntilUtc = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var key = LicenseSigner.Sign(expiredUpdates, priv);

        var result = new LicenseVerifier(pub).Verify(key);

        // ADR-002: an old UpdatesUntilUtc means "no more pack updates", NOT "Repair is locked".
        // Verify() only checks authenticity — the caller decides what to do with an expired
        // update window (e.g. skip offering a newer pack), it never invalidates the license.
        A.True(result.IsValid, "a license with a lapsed update window must still verify as a valid, unlocked license");
    }

    // ─────────────────────────────── the placeholder embedded key ───────────────────────────────
    // Audit 2026-08-04 caught the embedded EmbeddedPublicKeyBase64 constant as an invalid DER
    // string that crashed `new LicenseVerifier()` (the exact constructor the shipped App calls)
    // before Verify() ever ran. Fixed so a broken/placeholder key degrades to a loud "Invalid"
    // instead of crashing the app on startup. These tests lock that behaviour in — they must keep
    // passing even after Maxime pastes the real key, since they exercise the DEGRADATION path,
    // not the embedded constant's specific (and legitimately still-fake) value.

    public static void Test_ParameterlessConstructor_NeverThrows()
    {
        // This is exactly what the shipped App calls. Must never throw, placeholder or not.
        var verifier = new LicenseVerifier();
        var result = verifier.Verify("anything");
        A.False(result.IsValid, "an unconfigured/placeholder key must never validate a license");
    }

    public static void Test_GarbagePublicKey_DegradesToInvalid_NeverThrows()
    {
        foreach (var badKey in new[] { "", "not-base64!!!", "QQ==", new string('A', 4000) })
        {
            var verifier = new LicenseVerifier(badKey);   // must not throw during construction
            var result = verifier.Verify("irrelevant");
            A.False(result.IsValid, $"a malformed public key ('{badKey[..Math.Min(10, badKey.Length)]}...') must degrade to Invalid, not throw");
        }
    }

    /// <summary>
    /// Revue sécurité 2026-08-05 : borne de taille avant décodage — rejette vite un copier-coller
    /// massivement erroné (Mo de texte) au lieu de le décoder en mémoire pour rien. Un vrai
    /// bug le laisserait passer jusqu'au Base64UrlDecode/JSON, gaspillant mémoire/CPU sur une
    /// entrée qui échouera de toute façon.
    /// </summary>
    public static void Test_OversizedLicenseKey_RejectedBeforeDecoding()
    {
        var (_, pub) = NewKeypair();
        var verifier = new LicenseVerifier(pub);

        var oversized = new string('A', 10_000) + "." + new string('B', 10_000);
        var result = verifier.Verify(oversized);

        A.False(result.IsValid, "a wildly oversized license key must be rejected, not decoded");
    }

    /// <summary>
    /// Revue sécurité 2026-08-05 : ECDsa.Create() peut réussir puis ImportSubjectPublicKeyInfo
    /// lever ensuite — avant le fix, le handle crypto natif de `key` n'était jamais disposé sur ce
    /// chemin. C'est le chemin pris à CHAQUE démarrage tant que EmbeddedPublicKeyBase64 reste le
    /// placeholder. Pas de moyen simple d'observer une fuite de handle natif depuis un test — cette
    /// boucle vérifie au moins qu'instancier ce chemin d'échec en rafale ne dégrade rien
    /// d'observable (pas d'exception, pas de état incohérent).
    /// </summary>
    public static void Test_RepeatedFailedKeyImport_DoesNotThrowOrLeaveInconsistentState()
    {
        for (var i = 0; i < 50; i++)
        {
            var verifier = new LicenseVerifier("not-a-valid-der-key-at-all");
            A.False(verifier.Verify("anything").IsValid, "each failed-import instance must still degrade cleanly to Invalid");
        }
    }

    private static string FlipOneChar(string s)
    {
        var chars = s.ToCharArray();
        var i = chars.Length / 2;
        chars[i] = chars[i] == 'A' ? 'B' : 'A';
        return new string(chars);
    }
}
