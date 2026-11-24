using CoilPro.Cli.Commands;

namespace CoilPro.Cli;

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

            case "status":
                return await ReadCommands.StatusAsync(rest, ct).ConfigureAwait(false);

            case "cached":
                return await ReadCommands.CachedAsync(rest, ct).ConfigureAwait(false);

            case "alerts":
                return await ReadCommands.AlertsAsync(rest, ct).ConfigureAwait(false);

            case "release-notes":
                return await ReadCommands.ReleaseNotesAsync(rest, ct).ConfigureAwait(false);

            case "service":
                return await ReadCommands.ServiceAsync(rest, ct).ConfigureAwait(false);

            case "drivers":
                return await ReadCommands.DriversAsync(rest, ct).ConfigureAwait(false);

            case "nearby":
                return await ReadCommands.NearbyAsync(rest, ct).ConfigureAwait(false);

            case "charging-history":
                return await ReadCommands.ChargingHistoryAsync(rest, ct).ConfigureAwait(false);

            case "invoice":
                return await ReadCommands.InvoiceAsync(rest, ct).ConfigureAwait(false);

            case "me":
                return await ReadCommands.MeAsync(rest, ct).ConfigureAwait(false);

            case "region":
                return await ReadCommands.RegionAsync(rest, ct).ConfigureAwait(false);

            case "warranty":
                return await ReadCommands.WarrantyAsync(rest, ct).ConfigureAwait(false);

            case "options":
                return await ReadCommands.OptionsAsync(rest, ct).ConfigureAwait(false);

            case "energy":
                return await ReadCommands.EnergyAsync(rest, ct).ConfigureAwait(false);

            case "telemetry":
                return await ReadCommands.TelemetryAsync(rest, ct).ConfigureAwait(false);

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
            coilpro — CoilPro CLI (unofficial Tesla Fleet API SDK helper; not affiliated with Tesla, Inc.)

            Usage:
              coilpro <command> [options]

            Setup:
              keys        Generate the P-256 keypair for partner registration / signed commands.
              partner     Register your application with Tesla, inspect the registered key.
              login       Run the OAuth authorization-code + PKCE flow via a local loopback redirect.

            Vehicle data (free unless marked BILLED; the Fleet API is pay-per-use):
              vehicles          List the vehicles on the account.
              data <VIN>        Live vehicle data snapshot (BILLED: device data; vehicle must be online).
              cached <VIN>      Last data the vehicle reported; works while asleep, no location.
              status <VIN>      Key paired, firmware, protocol requirement (fleet_status).
              alerts <VIN>      Recent vehicle alerts.
              release-notes <VIN> [--staged]   Release notes of installed or staged firmware.
              service <VIN>     Service status and service history.
              drivers <VIN>     Drivers with access (owner only).
              nearby <VIN>      Nearby Superchargers (BILLED: device data; vehicle must be online).
              warranty <VIN>    Warranty coverage.        options <VIN>   Option codes.
              wake <VIN>        Wake a sleeping vehicle (BILLED: wake).

            Account and charging:
              me                Profile of the logged-in user.     region   Fleet API region of the user.
              charging-history [--vin <VIN>] [--from <date>] [--to <date>] [--page-size <n>]
              invoice <contentId> [--out <file.pdf>]    Download a Supercharger invoice.

            Energy and telemetry:
              energy products | energy live <siteId>    Powerwall / solar / Wall Connector sites.
              telemetry get|errors|delete <VIN>         Fleet Telemetry configuration of a vehicle.

            Commands:
              cmd <VIN> <command> [args]    Send a signed vehicle command (BILLED: commands). About 70
                                            commands plus navigation; see `coilpro cmd --help`.

            First-time setup:
              coilpro keys generate
              # publish the public key at https://<domain>/.well-known/appspecific/com.tesla.3p.public-key.pem
              coilpro partner register --domain <domain> --client-secret <secret>
              coilpro login

            'keys' / 'partner' options:
              Run `coilpro keys --help` or `coilpro partner --help`.

            'login' options:
              --client-id <id>          OAuth client id                       (or env COILPRO_CLIENT_ID)
              --client-secret <secret>  OAuth client secret, if confidential   (or env COILPRO_CLIENT_SECRET)
              --region <eu|na>          Fleet API region, default: eu         (or env COILPRO_REGION)
              --scopes <s1,s2,...>      Comma-separated scopes, default: openid,offline_access,user_data,
                                          vehicle_device_data,vehicle_location,vehicle_cmds,
                                          vehicle_charging_cmds,energy_device_data,energy_cmds
                                        (also accepted: vehicle_specs, vehicle_pricing_info,
                                          enterprise_management)
              --port <n>                Fixed loopback port (default: an ephemeral free port)
              --timeout <seconds>       Seconds to wait for the browser callback, default: 120

            'data' options:
              <vin>                     Vehicle id or VIN (required)
              --endpoints <e1,e2,...>   Data segments to request, default: charge_state,drive_state,
                                        climate_state,vehicle_state

            'cmd' usage:
              coilpro cmd <VIN> <command> [args] [--wake] [--timeout <seconds>]
              Examples: lock | flash | charge-limit 80 | temps 21 21 | trunk | sentry on |
                        charge-schedule-add weekdays 23:00-06:00 52.52 13.40 | nav "Alexanderplatz, Berlin"
              Run `coilpro cmd --help` for all commands. <VIN> is the 17-character VIN (not the numeric id).
              --wake                    Wake a sleeping vehicle first (billed Fleet API call)
              --timeout <seconds>       Overall command deadline, default: 30
              Needs `coilpro keys generate`, a paired virtual key and a prior `coilpro login`
              (navigation commands need no key).
              Exit codes: 0 ok, 1 error, 2 refused by vehicle, 3 key not paired, 4 vehicle asleep.

            Global:
              --help, -h                Show this help.
            """);
    }
}
