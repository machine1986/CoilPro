using Coil.Cli.Commands;

namespace Coil.Cli;

/// <summary>Top-level command dispatcher for the <c>coil</c> CLI. Manual arg parsing — no external CLI framework.</summary>
internal static class CliApp
{
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var command = args[0];
        var rest = args[1..];

        if (IsHelpFlag(command))
        {
            PrintUsage();
            return 0;
        }

        switch (command.ToLowerInvariant())
        {
            case "login":
                return await LoginCommand.RunAsync(rest, ct).ConfigureAwait(false);

            case "keys":
                PrintComingSoon("coil keys", "M3", "generates a Vehicle Command Protocol signing keypair (EC P-256) and the .well-known PEM file.");
                return 0;

            case "partner":
                PrintComingSoon("coil partner", "M3", "registers your application as a Tesla Fleet API partner (domain + public key).");
                return 0;

            case "vehicles":
                return await VehiclesCommand.RunAsync(rest, ct).ConfigureAwait(false);

            case "data":
                return await DataCommand.RunAsync(rest, ct).ConfigureAwait(false);

            default:
                Console.Error.WriteLine($"Unknown command: '{command}'.");
                Console.Error.WriteLine();
                PrintUsage(Console.Error);
                return 1;
        }
    }

    private static bool IsHelpFlag(string arg) =>
        arg is "--help" or "-h" or "-?" or "help";

    private static void PrintComingSoon(string command, string milestone, string description)
    {
        Console.WriteLine($"'{command}' is coming in {milestone}.");
        Console.WriteLine($"  {description}");
    }

    private static void PrintUsage(TextWriter? writer = null)
    {
        writer ??= Console.Out;
        writer.WriteLine(
            """
            coil — Coil.NET CLI (unofficial Tesla Fleet API SDK helper; not affiliated with Tesla, Inc.)

            Usage:
              coil <command> [options]

            Commands:
              login       Run the OAuth authorization-code + PKCE flow via a local loopback redirect.
              vehicles    List the vehicles on the authenticated account.
              data        Fetch a vehicle data snapshot (battery, drive, climate, vehicle state).
              keys        Generate a Vehicle Command signing keypair.        (coming in M3)
              partner     Register / manage your Tesla partner account.      (coming in M3)

            'login' options:
              --client-id <id>          OAuth client id                       (or env COIL_CLIENT_ID)
              --client-secret <secret>  OAuth client secret, if confidential   (or env COIL_CLIENT_SECRET)
              --region <eu|na>          Fleet API region, default: eu         (or env COIL_REGION)
              --scopes <s1,s2,...>      Comma-separated scopes, default: openid,offline_access
              --port <n>                Fixed loopback port (default: an ephemeral free port)
              --timeout <seconds>       Seconds to wait for the browser callback, default: 120

            'data' options:
              <vin>                     Vehicle id or VIN (required)
              --endpoints <e1,e2,...>   Data segments to request, default: charge_state,drive_state,
                                        climate_state,vehicle_state

            Global:
              --help, -h                Show this help.
            """);
    }
}
