using CoilPro.Cli.Internal;

namespace CoilPro.Cli.Commands;

/// <summary>
/// Implements <c>coilpro partner</c>: the one-time partner-account registration every Fleet API
/// application must complete, plus a check of the public key Tesla currently has on file.
/// </summary>
/// <remarks>
/// These endpoints authenticate with a partner token (OAuth client-credentials), so unlike
/// <c>coilpro vehicles</c>/<c>coilpro data</c> they need the client SECRET — which the CLI deliberately
/// never persists. Pass <c>--client-secret</c> or set <c>COILPRO_CLIENT_SECRET</c>.
/// </remarks>
internal static class PartnerCommand
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
            "register" => await RegisterAsync(rest, ct).ConfigureAwait(false),
            "public-key" or "publickey" => await PublicKeyAsync(rest, ct).ConfigureAwait(false),
            _ => UnknownSubcommand(subcommand),
        };
    }

    private static async Task<int> RegisterAsync(string[] args, CancellationToken ct)
    {
        using var context = BuildContext("coilpro partner register", args);
        if (context is null)
        {
            return 1;
        }

        Console.WriteLine($"Registering '{context.Domain}' in region {context.Region}...");
        Console.WriteLine();

        try
        {
            var account = await context.PartnerClient.RegisterAsync(context.Domain, ct).ConfigureAwait(false);

            Console.WriteLine("Registration succeeded.");
            Console.WriteLine($"  Domain:     {account.Domain ?? context.Domain}");
            if (account.Name is not null)
            {
                Console.WriteLine($"  Name:       {account.Name}");
            }

            if (account.AccountId is not null)
            {
                Console.WriteLine($"  Account id: {account.AccountId}");
            }

            if (account.ClientId is not null)
            {
                Console.WriteLine($"  Client id:  {account.ClientId}");
            }

            Console.WriteLine();
            PrintRegionReminder(context.Region);
            Console.WriteLine();
            Console.WriteLine("Verify what Tesla stored:");
            Console.WriteLine($"  coilpro partner public-key --domain {context.Domain} --region {RegionFlag(context.Region)}");

            RateLimitPrinter.Print(context.PartnerClient.LastRateLimit);
            return 0;
        }
        catch (TeslaApiException ex)
        {
            Console.Error.WriteLine($"coilpro partner register: {VehiclesCommand.DescribeApiError(ex)}");
            PrintRegistrationHints(context.Domain);
            return 1;
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"coilpro partner register: could not obtain a partner token: {ex.Message}");
            Console.Error.WriteLine("Check that --client-id and --client-secret belong to the same application.");
            return 1;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Console.Error.WriteLine("coilpro partner register: cancelled.");
            return 130;
        }
    }

    private static async Task<int> PublicKeyAsync(string[] args, CancellationToken ct)
    {
        using var context = BuildContext("coilpro partner public-key", args);
        if (context is null)
        {
            return 1;
        }

        try
        {
            var registered = await context.PartnerClient.GetPublicKeyAsync(context.Domain, ct).ConfigureAwait(false);

            if (registered is null)
            {
                Console.WriteLine($"Tesla has no public key on file for '{context.Domain}' in region {context.Region}.");
                Console.WriteLine("Run `coilpro partner register` first.");
                RateLimitPrinter.Print(context.PartnerClient.LastRateLimit);
                return 1;
            }

            Console.WriteLine($"Public key registered for '{context.Domain}' ({context.Region}):");
            Console.WriteLine($"  {registered}");

            CompareWithLocalKey(registered);

            RateLimitPrinter.Print(context.PartnerClient.LastRateLimit);
            return 0;
        }
        catch (TeslaApiException ex)
        {
            Console.Error.WriteLine($"coilpro partner public-key: {VehiclesCommand.DescribeApiError(ex)}");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"coilpro partner public-key: could not obtain a partner token: {ex.Message}");
            return 1;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Console.Error.WriteLine("coilpro partner public-key: cancelled.");
            return 130;
        }
    }

    /// <summary>
    /// Compares what Tesla has on file with the locally generated key, so a stale or mismatched
    /// registration is caught here rather than as a confusing signing failure later.
    /// </summary>
    private static void CompareWithLocalKey(string registered)
    {
        if (!File.Exists(CoilProPaths.PrivateKeyFile))
        {
            return;
        }

        try
        {
            using var keyPair = TeslaKeyPair.FromPrivateKeyPem(File.ReadAllText(CoilProPaths.PrivateKeyFile));
            var local = keyPair.ExportPublicKeyHex();

            Console.WriteLine();
            if (string.Equals(local, registered.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Matches the local key pair at {CoilProPaths.PrivateKeyFile}.");
            }
            else
            {
                Console.WriteLine($"WARNING: does NOT match the local key pair at {CoilProPaths.PrivateKeyFile}:");
                Console.WriteLine($"  local: {local}");
                Console.WriteLine("Re-publish the local public key at the well-known URL and re-run `coilpro partner register`.");
            }
        }
        catch (Exception ex) when (ex is ArgumentException or IOException)
        {
            // Comparing is a convenience; an unreadable local key must not fail the command.
            Console.WriteLine();
            Console.WriteLine($"(Could not read the local key pair for comparison: {ex.Message})");
        }
    }

    /// <summary>
    /// Parses the shared partner options and builds a ready-to-use client, or reports the problem to
    /// stderr and returns null.
    /// </summary>
    private static PartnerContext? BuildContext(string command, string[] args)
    {
        CliOptions options;
        try
        {
            options = CliOptions.Parse(args);
        }
        catch (CliUsageException ex)
        {
            Console.Error.WriteLine($"{command}: {ex.Message}");
            return null;
        }

        // Fall back to the client id captured at login, so a prior `coilpro login` is enough.
        var session = CliSession.LoadAsync().GetAwaiter().GetResult();

        var clientId = options.ClientId ?? session?.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            Console.Error.WriteLine($"{command}: missing client id. Pass --client-id or set COILPRO_CLIENT_ID.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            Console.Error.WriteLine($"{command}: missing client secret. Pass --client-secret or set COILPRO_CLIENT_SECRET.");
            Console.Error.WriteLine("Partner endpoints use the client-credentials flow; the CLI never persists the secret.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(options.Domain))
        {
            Console.Error.WriteLine($"{command}: missing domain. Pass --domain or set COILPRO_DOMAIN.");
            return null;
        }

        try
        {
            var domain = TeslaPartnerClient.NormalizeDomain(options.Domain);
            return PartnerContext.Create(clientId, options.ClientSecret, options.Region, domain);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"{command}: {StripParameterSuffix(ex.Message)}");
            return null;
        }
    }

    /// <summary>
    /// Drops the <c>(Parameter 'x')</c> tail <see cref="ArgumentException"/> appends — useful in a
    /// stack trace, noise in a message aimed at someone typing a command.
    /// </summary>
    private static string StripParameterSuffix(string message)
    {
        var index = message.IndexOf(" (Parameter '", StringComparison.Ordinal);
        return index < 0 ? message : message[..index];
    }

    private static void PrintRegionReminder(TeslaRegion region)
    {
        var other = region == TeslaRegion.Europe ? TeslaRegion.NorthAmerica : TeslaRegion.Europe;
        Console.WriteLine("Registration applies to ONE region only. If your application also serves");
        Console.WriteLine($"{other}, register there too — otherwise those calls fail with region errors");
        Console.WriteLine("even when the region is otherwise correct:");
        Console.WriteLine($"  coilpro partner register --domain <your-domain> --region {RegionFlag(other)}");
    }

    private static void PrintRegistrationHints(string domain)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("Common causes:");
        Console.Error.WriteLine($"  - The public key is not reachable at https://{domain}/{TeslaKeyPair.WellKnownPublicKeyPath}");
        Console.Error.WriteLine("    (it must be served over HTTPS, publicly, with no redirect to a login page).");
        Console.Error.WriteLine("  - The key is not a P-256 (secp256r1) key — regenerate with `coilpro keys generate`.");
        Console.Error.WriteLine($"  - '{domain}' does not match the root domain of an Allowed Origin on developer.tesla.com.");
    }

    private static string RegionFlag(TeslaRegion region) => region == TeslaRegion.Europe ? "eu" : "na";

    private static int UnknownSubcommand(string subcommand)
    {
        Console.Error.WriteLine($"coilpro partner: unknown subcommand '{subcommand}'.");
        Console.Error.WriteLine();
        PrintUsage(Console.Error);
        return 1;
    }

    private static bool IsHelpFlag(string arg) => arg is "--help" or "-h" or "-?";

    private static void PrintUsage(TextWriter? writer = null)
    {
        writer ??= Console.Out;
        writer.WriteLine(
            $"""
            coilpro partner — register this application with Tesla and inspect the registered key.

            Usage:
              coilpro partner register   --domain <domain> [--region <eu|na>] [--client-id <id>] [--client-secret <secret>]
              coilpro partner public-key --domain <domain> [--region <eu|na>] [--client-id <id>] [--client-secret <secret>]

            Options:
              --domain <domain>         Bare host serving your public key    (or env COILPRO_DOMAIN)
              --region <eu|na>          Region to register in, default: eu   (or env COILPRO_REGION)
              --client-id <id>          OAuth client id                      (or env COILPRO_CLIENT_ID)
              --client-secret <secret>  OAuth client secret, required        (or env COILPRO_CLIENT_SECRET)

            Before registering, publish your public key at:
              https://<domain>/{TeslaKeyPair.WellKnownPublicKeyPath}
            Generate one with `coilpro keys generate`.

            Registration is per region — run it once per region you serve.
            """);
    }

    /// <summary>Bundles the partner client with the HTTP clients backing it, for a single <c>using</c>.</summary>
    private sealed class PartnerContext(
        HttpClient authHttp,
        HttpClient apiHttp,
        ITeslaPartnerClient partnerClient,
        TeslaRegion region,
        string domain) : IDisposable
    {
        public ITeslaPartnerClient PartnerClient { get; } = partnerClient;

        public TeslaRegion Region { get; } = region;

        public string Domain { get; } = domain;

        public static PartnerContext Create(string clientId, string? clientSecret, TeslaRegion region, string domain)
        {
            var authOptions = new TeslaAuthOptions
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                Region = region,
                // The partner token needs data/command scopes, not the authorization-code default
                // (openid offline_access): with that, GET partner_accounts/public_key answers
                // 403 "Unauthorized missing scopes". Scope set as in Tesla's partner-token example.
                Scopes = TeslaScopes.Openid | TeslaScopes.VehicleDeviceData | TeslaScopes.VehicleCmds | TeslaScopes.VehicleChargingCmds,
            };

            var authHttp = new HttpClient();
            ITeslaAuthClient authClient = new TeslaAuthClient(authHttp, authOptions);

            var apiHttp = new HttpClient { BaseAddress = TeslaEndpoints.For(region).FleetApiBaseUrl };
            ITeslaPartnerClient partnerClient = new TeslaPartnerClient(apiHttp, authClient);

            return new PartnerContext(authHttp, apiHttp, partnerClient, region, domain);
        }

        public void Dispose()
        {
            authHttp.Dispose();
            apiHttp.Dispose();
        }
    }
}
