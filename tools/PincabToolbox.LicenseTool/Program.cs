using System.Security.Cryptography;
using PincabToolbox.Repair.Licensing;

namespace PincabToolbox.LicenseTool;

/// <summary>
/// Runs OFFLINE, on Maxime's own machine only. Never ships inside the App — it is the only place
/// that ever touches the license PRIVATE key. See README.md in this folder for the full workflow.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0) return Usage();

        try
        {
            return args[0] switch
            {
                "init" => Init(args[1..]),
                "issue" => Issue(args[1..]),
                "verify" => Verify(args[1..]),
                _ => Usage(),
            };
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"error: {e.Message}");
            return 1;
        }
    }

    // ─────────────────────────────── init ───────────────────────────────

    private static int Init(string[] args)
    {
        var outPath = Opt(args, "--out") ?? "license-private-key.pem";
        var force = args.Contains("--force");

        if (File.Exists(outPath) && !force)
        {
            Console.Error.WriteLine($"error: {outPath} already exists. This would invalidate every " +
                "key already sold if overwritten by accident. Pass --force if you really mean it.");
            return 1;
        }

        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var privatePem = key.ExportECPrivateKeyPem();
        File.WriteAllText(outPath, privatePem);

        var publicKeyBase64 = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo());

        Console.WriteLine("New license keypair generated.");
        Console.WriteLine();
        Console.WriteLine($"  Private key written to : {Path.GetFullPath(outPath)}");
        Console.WriteLine("  ⚠ Keep this file OFF the git repo, backed up somewhere safe (password");
        Console.WriteLine("    manager, encrypted drive). Anyone who has it can mint valid licenses.");
        Console.WriteLine("    If it is ever lost, every key already sold keeps working — but you can");
        Console.WriteLine("    never sign a new one with this identity again.");
        Console.WriteLine();
        Console.WriteLine("  Public key (safe to publish — paste into LicenseVerifier.cs):");
        Console.WriteLine();
        Console.WriteLine($"    {publicKeyBase64}");
        Console.WriteLine();
        Console.WriteLine("  Next step: replace LicenseVerifier.EmbeddedPublicKeyBase64 in");
        Console.WriteLine("  src/PincabToolbox.Repair/Licensing/LicenseVerifier.cs with the line above,");
        Console.WriteLine("  then rebuild. From that point on, only THIS private key can produce a");
        Console.WriteLine("  license the shipped App will accept.");
        return 0;
    }

    // ─────────────────────────────── issue ───────────────────────────────

    private static int Issue(string[] args)
    {
        var keyPath = Opt(args, "--key");
        var email = Opt(args, "--email");
        if (keyPath is null || email is null)
        {
            Console.Error.WriteLine("usage: license-tool issue --key <private-key.pem> --email <email> [--updates-months 1200]");
            return 1;
        }
        if (!File.Exists(keyPath))
        {
            Console.Error.WriteLine($"error: private key file not found: {keyPath}");
            return 1;
        }

        // ADR-013 (19/08/2026) : mises à jour incluses sans limite de durée. 1200 mois (100 ans) est
        // le défaut décidé par Maxime pour représenter "sans limite" en pratique, sans réécrire tout
        // le modèle de licence (qui reste une date de fin de fenêtre de mise à jour).
        var months = int.TryParse(Opt(args, "--updates-months"), out var m) ? m : 1200;

        using var key = ECDsa.Create();
        key.ImportFromPem(File.ReadAllText(keyPath));

        var now = DateTimeOffset.UtcNow;
        var payload = new LicensePayload
        {
            Email = email.Trim(),
            IssuedUtc = now,
            UpdatesUntilUtc = now.AddMonths(months),
        };

        var licenseKey = LicenseSigner.Sign(payload, key);

        Console.WriteLine($"Licence émise pour {payload.Email}");
        Console.WriteLine($"  Mises à jour incluses jusqu'au : {payload.UpdatesUntilUtc:yyyy-MM-dd}");
        Console.WriteLine();
        Console.WriteLine("Clé de licence (à envoyer au client après paiement Stripe, ADR-013) :");
        Console.WriteLine();
        Console.WriteLine(licenseKey);
        return 0;
    }

    // ─────────────────────────────── verify (local sanity check) ───────────────────────────────

    private static int Verify(string[] args)
    {
        var pub = Opt(args, "--public-key");
        var license = Opt(args, "--license");
        if (pub is null || license is null)
        {
            Console.Error.WriteLine("usage: license-tool verify --public-key <base64> --license <key>");
            return 1;
        }

        var verifier = new LicenseVerifier(pub);
        var result = verifier.Verify(license);

        if (!result.IsValid)
        {
            Console.WriteLine($"INVALIDE — {result.Error}");
            return 1;
        }

        Console.WriteLine("VALIDE");
        Console.WriteLine($"  email              : {result.Payload!.Email}");
        Console.WriteLine($"  émise le           : {result.Payload.IssuedUtc:yyyy-MM-dd}");
        Console.WriteLine($"  MAJ incluses jusqu'au : {result.Payload.UpdatesUntilUtc:yyyy-MM-dd}");
        return 0;
    }

    // ─────────────────────────────── plumbing ───────────────────────────────

    private static string? Opt(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private static int Usage()
    {
        Console.WriteLine("PincabToolbox.LicenseTool — génère et signe des licences Repair. OFFLINE UNIQUEMENT.");
        Console.WriteLine();
        Console.WriteLine("  license-tool init [--out private-key.pem] [--force]");
        Console.WriteLine("      Génère une nouvelle paire de clés. À faire UNE SEULE FOIS.");
        Console.WriteLine();
        Console.WriteLine("  license-tool issue --key private-key.pem --email client@exemple.com [--updates-months 1200]");
        Console.WriteLine("      Émet une clé de licence pour un client, à envoyer après paiement.");
        Console.WriteLine();
        Console.WriteLine("  license-tool verify --public-key <base64> --license <clé>");
        Console.WriteLine("      Vérifie une clé localement, sans lancer l'App — utile pour tester.");
        return 1;
    }
}
