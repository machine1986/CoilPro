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
                return await KeysCommand.RunAsync(rest, ct).ConfigureAwait(false);

            case "partner":
                return await PartnerCommand.RunAsync(rest, ct).ConfigureAwait(false);

            case "vehicles":
                return await VehiclesCommand.RunAsync(rest, ct).ConfigureAwait(false);

            case "data":
                return await DataCommand.RunAsync(rest, ct).ConfigureAwait(false);

            case "wake":
                return await WakeCommand.RunAsync(rest, ct).ConfigureAwait(false);

            case "cmd":
                return await CmdCommand.RunAsync(rest, ct).ConfigureAwait(false);

            default:
                Console.Error.WriteLine($"Unknown command: '{command}'.");
                Console.Error.WriteLine();
                PrintUsage(Console.Error);
                return 1;
        }
    }

    private static bool IsHelpFlag(string arg) =>
        arg is "--help" or "-h" or "-?" or "help";

    private static void PrintUsage(TextWriter? writer = null)
    {
        writer ??= Console.Out;
        writer.WriteLine(
            """
            coil — Coil.NET CLI (unofficial Tesla Fleet API SDK helper; not affiliated with Tesla, Inc.)

            Usage:
              coil <command> [options]

            Commands:
              keys        Generate the P-256 keypair for partner registration / signed commands.
              partner     Register your application with Tesla, inspect the registered key.
              login       Run the OAuth authorization-code + PKCE flow via a local loopback redirect.
              vehicles    List the vehicles on the authenticated account.
              data        Fetch a vehicle data snapshot (battery, drive, climate, vehicle state).
              wake        Wake a sleeping vehicle.
              cmd         Send a signed vehicle command (lock, flash, charge-limit, ...).

            First-time setup:
              coil keys generate
              # publish the public key at https://<domain>/.well-known/appspecific/com.tesla.3p.public-key.pem
              coil partner register --domain <domain> --client-secret <secret>
              coil login

            'keys' / 'partner' options:
              Run `coil keys --help` or `coil partner --help`.

            'login' options:
              --client-id <id>          OAuth client id                       (or env COIL_CLIENT_ID)
              --client-secret <secret>  OAuth client secret, if confidential   (or env COIL_CLIENT_SECRET)
              --region <eu|na>          Fleet API region, default: eu         (or env COIL_REGION)
              --scopes <s1,s2,...>      Comma-separated scopes, default: openid,offline_access,user_data,
                                          vehicle_device_data,vehicle_location,vehicle_cmds,
                                          vehicle_charging_cmds
              --port <n>                Fixed loopback port (default: an ephemeral free port)
              --timeout <seconds>       Seconds to wait for the browser callback, default: 120

            'data' options:
              <vin>                     Vehicle id or VIN (required)
              --endpoints <e1,e2,...>   Data segments to request, default: charge_state,drive_state,
                                        climate_state,vehicle_state

            'cmd' usage:
              coil cmd <VIN> <command> [args] [--wake] [--timeout <seconds>]
              Commands: lock, unlock, honk, flash, climate-on, climate-off, temps <driverC> <passengerC>,
                        charge-start, charge-stop, charge-limit <percent>, charge-port-open,
                        charging-amps <amps>
              <VIN>                     17-character VIN (not the numeric id)
              --wake                    Wake a sleeping vehicle first (billed Fleet API call)
              --timeout <seconds>       Overall command deadline, default: 30
              Needs `coil keys generate`, a paired virtual key and a prior `coil login`.
              Exit codes: 0 ok, 1 error, 2 refused by vehicle, 3 key not paired, 4 vehicle asleep.

            Global:
              --help, -h                Show this help.
            """);
    }
}
