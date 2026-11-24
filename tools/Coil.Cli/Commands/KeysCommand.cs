using Coil.Cli.Internal;

namespace Coil.Cli.Commands;

/// <summary>
/// Implements <c>coil keys</c>: generates and inspects the P-256 key pair Tesla requires for
/// partner registration and the Vehicle Command Protocol.
/// </summary>
/// <remarks>
/// The private key is written owner-readable and must never be committed or published; only the
/// public PEM is served from the application domain.
/// </remarks>
internal static class KeysCommand
{
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        if (args.Length == 0 || args.Any(IsHelpFlag))
        {
            PrintUsage();
            return args.Length == 0 ? 1 : 0;
        }

        var subcommand = args[0].ToLowerInvariant();
        var rest = args[1..];

        return subcommand switch
        {
            "generate" or "gen" => await GenerateAsync(rest, ct).ConfigureAwait(false),
            "show" => Show(rest),
            _ => UnknownSubcommand(subcommand),
        };
    }

    private static async Task<int> GenerateAsync(string[] args, CancellationToken ct)
    {
        string? outDir = null;
        var force = false;

        for (var i = 0; i < args.Length; i++)
        {
            var (name, inline) = SplitFlag(args[i]);
            switch (name)
            {
                case "--out":
                    if (inline is not null)
                    {
                        outDir = inline;
                    }
                    else if (i + 1 < args.Length)
                    {
                        outDir = args[++i];
                    }
                    else
                    {
                        Console.Error.WriteLine("coil keys generate: missing value for --out.");
                        return 1;
                    }

                    break;
                case "--force":
                    force = true;
                    break;
                default:
                    Console.Error.WriteLine($"coil keys generate: unknown option '{args[i]}'.");
                    return 1;
            }
        }

        var privatePath = outDir is null ? CoilPaths.PrivateKeyFile : Path.Combine(outDir, "private-key.pem");
        var publicPath = outDir is null ? CoilPaths.PublicKeyFile : Path.Combine(outDir, "public-key.pem");

        if (!force && File.Exists(privatePath))
        {
            Console.Error.WriteLine($"coil keys generate: a private key already exists at {privatePath}.");
            Console.Error.WriteLine("Refusing to overwrite it — a lost signing key means re-pairing every vehicle.");
            Console.Error.WriteLine("Pass --force if you really want to replace it.");
            return 1;
        }

        using var keyPair = TeslaKeyPair.Generate();

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(privatePath)!);
            await AtomicFileWriter.WriteAsync(privatePath, keyPair.ExportPrivateKeyPem(), ct).ConfigureAwait(false);
            // Public by design and meant to be uploaded — 0644, so `sftp put` doesn't carry 0600 onto the web server.
            await AtomicFileWriter.WriteAsync(publicPath, keyPair.ExportPublicKeyPem(), ct, AtomicFileWriter.WorldReadable).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"coil keys generate: could not write the key files: {ex.Message}");
            return 1;
        }

        Console.WriteLine("Generated a P-256 (secp256r1) key pair.");
        Console.WriteLine($"  Private key: {privatePath}   (secret — never commit or publish this)");
        Console.WriteLine($"  Public key:  {publicPath}");
        Console.WriteLine();
        PrintNextSteps(publicPath);
        return 0;
    }

    private static int Show(string[] args)
    {
        if (args.Length > 0)
        {
            Console.Error.WriteLine($"coil keys show: unexpected argument '{args[0]}'.");
            return 1;
        }

        if (!File.Exists(CoilPaths.PrivateKeyFile))
        {
            Console.Error.WriteLine($"coil keys show: no key pair at {CoilPaths.PrivateKeyFile}. Run `coil keys generate` first.");
            return 1;
        }

        TeslaKeyPair keyPair;
        try
        {
            keyPair = TeslaKeyPair.FromPrivateKeyPem(File.ReadAllText(CoilPaths.PrivateKeyFile));
        }
        catch (Exception ex) when (ex is ArgumentException or IOException)
        {
            Console.Error.WriteLine($"coil keys show: could not read the key pair: {ex.Message}");
            return 1;
        }

        using (keyPair)
        {
            Console.WriteLine($"Private key: {CoilPaths.PrivateKeyFile}");
            Console.WriteLine($"Public key:  {CoilPaths.PublicKeyFile}");
            Console.WriteLine($"Curve:       P-256 (secp256r1)");
            Console.WriteLine();
            Console.WriteLine("Public key (hex, uncompressed point) — this is what Tesla echoes back from");
            Console.WriteLine("`coil partner public-key`, so the two should match exactly:");
            Console.WriteLine($"  {keyPair.ExportPublicKeyHex()}");
            Console.WriteLine();
            Console.WriteLine("Public key (PEM) — serve exactly this content:");
            Console.WriteLine(keyPair.ExportPublicKeyPem().TrimEnd());
        }

        return 0;
    }

    private static void PrintNextSteps(string publicPath)
    {
        Console.WriteLine("Next steps:");
        Console.WriteLine($"  1. Publish {Path.GetFileName(publicPath)} so that this URL serves it over HTTPS,");
        Console.WriteLine("     publicly and without authentication:");
        Console.WriteLine($"       https://<your-domain>/{TeslaKeyPair.WellKnownPublicKeyPath}");
        Console.WriteLine("     The domain must match the root domain of an Allowed Origin on developer.tesla.com.");
        Console.WriteLine("  2. Register it:");
        Console.WriteLine("       coil partner register --domain <your-domain> --client-secret <secret>");
        Console.WriteLine("  3. Have the driver install the Virtual Key in the Tesla app:");
        Console.WriteLine("       https://tesla.com/_ak/<your-domain>");
    }

    private static int UnknownSubcommand(string subcommand)
    {
        Console.Error.WriteLine($"coil keys: unknown subcommand '{subcommand}'.");
        Console.Error.WriteLine();
        PrintUsage(Console.Error);
        return 1;
    }

    private static (string Name, string? InlineValue) SplitFlag(string arg)
    {
        var eq = arg.IndexOf('=');
        return eq < 0 ? (arg, null) : (arg[..eq], arg[(eq + 1)..]);
    }

    private static bool IsHelpFlag(string arg) => arg is "--help" or "-h" or "-?";

    private static void PrintUsage(TextWriter? writer = null)
    {
        writer ??= Console.Out;
        writer.WriteLine(
            $"""
            coil keys — manage the P-256 key pair used for partner registration and signed commands.

            Usage:
              coil keys generate [--out <dir>] [--force]
              coil keys show

            'generate' options:
              --out <dir>   Directory to write the key pair to. Default: {CoilPaths.KeysDir}
              --force       Overwrite an existing private key. Refused by default.

            The public key must be served at:
              https://<your-domain>/{TeslaKeyPair.WellKnownPublicKeyPath}
            """);
    }
}
