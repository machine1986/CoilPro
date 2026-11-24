using System.Globalization;
using CoilPro.Cli.Internal;
using CoilPro.Commands;

namespace CoilPro.Cli.Commands;

/// <summary>
/// Implements <c>coilpro cmd &lt;VIN&gt; &lt;command&gt; [args]</c>: sends one signed vehicle command via the
/// Vehicle Command Protocol (<see cref="CoilProCommands"/>).
/// </summary>
/// <remarks>
/// Exit codes: 0 success, 1 usage/other error, 2 refused by the vehicle, 3 key not paired,
/// 4 vehicle asleep, 130 cancelled.
/// </remarks>
internal static class CmdCommand
{
    private const int ExitError = 1;
    private const int ExitRefused = 2;
    private const int ExitKeyNotPaired = 3;
    private const int ExitAsleep = 4;

    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        var positional = new List<string>();
        var wake = false;
        var debug = false;
        var once = false;
        var fanOnly = false;
        ulong? scheduleId = null;
        var timeout = TimeSpan.FromSeconds(30);

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg is "--help" or "-h" or "-?")
            {
                PrintUsage();
                return 0;
            }

            if (arg == "--wake")
            {
                wake = true;
            }
            else if (arg == "--debug")
            {
                debug = true;
            }
            else if (arg == "--once")
            {
                once = true;
            }
            else if (arg == "--fan-only")
            {
                fanOnly = true;
            }
            else if (arg == "--id" || arg.StartsWith("--id=", StringComparison.Ordinal))
            {
                string idText;
                if (arg.Contains('='))
                {
                    idText = arg[(arg.IndexOf('=') + 1)..];
                }
                else if (i + 1 < args.Length)
                {
                    idText = args[++i];
                }
                else
                {
                    return Usage("missing value for --id.");
                }

                if (!ulong.TryParse(idText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedId))
                {
                    return Usage($"invalid --id '{idText}' (expected a number).");
                }

                scheduleId = parsedId;
            }
            else if (arg == "--timeout" || arg.StartsWith("--timeout=", StringComparison.Ordinal))
            {
                string value;
                if (arg.Contains('='))
                {
                    value = arg[(arg.IndexOf('=') + 1)..];
                }
                else if (i + 1 < args.Length)
                {
                    value = args[++i];
                }
                else
                {
                    return Usage("missing value for --timeout.");
                }

                if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) || seconds <= 0)
                {
                    return Usage($"invalid --timeout '{value}' (expected seconds > 0).");
                }

                timeout = TimeSpan.FromSeconds(seconds);
            }
            else if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                return Usage($"unknown option '{arg}'.");
            }
            else
            {
                // Negative numbers (e.g. `temps -5 3`) stay positional on purpose.
                positional.Add(arg);
            }
        }

        if (positional.Count < 2)
        {
            Console.Error.WriteLine("coilpro cmd: expected <VIN> <command>.");
            Console.Error.WriteLine();
            PrintUsage(Console.Error);
            return ExitError;
        }

        var vin = positional[0];
        var name = positional[1].ToLowerInvariant();
        var cmdArgs = positional.Skip(2).ToArray();

        if (!IsVin(vin))
        {
            Console.Error.WriteLine(
                vin.All(char.IsDigit)
                    ? $"coilpro cmd: '{vin}' looks like the numeric vehicle id. Signed commands need the 17-character VIN — see `coilpro vehicles`."
                    : $"coilpro cmd: '{vin}' is not a valid VIN (expected 17 characters). See `coilpro vehicles`.");
            return ExitError;
        }

        vin = vin.ToUpperInvariant();

        ResolvedCommand resolved;
        try
        {
            resolved = CmdResolver.Resolve(name, cmdArgs, vin, new CmdFlags(scheduleId, once, fanOnly));
        }
        catch (CliUsageException ex)
        {
            return Usage(ex.Message);
        }

        // Unsigned REST commands (navigation) need no key; signed commands do.
        TeslaKeyPair? keyPair = null;
        if (resolved.Signed is not null)
        {
            try
            {
                if (!File.Exists(CoilProPaths.PrivateKeyFile))
                {
                    Console.Error.WriteLine($"coilpro cmd: no private key at {CoilProPaths.PrivateKeyFile}. Run `coilpro keys generate` first.");
                    return ExitError;
                }

                keyPair = TeslaKeyPair.FromPrivateKeyPem(File.ReadAllText(CoilProPaths.PrivateKeyFile));
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"coilpro cmd: could not read the private key: {ex.Message}");
                return ExitError;
            }
        }

        using (keyPair)
        {
            var (session, tokens) = await VehiclesCommand.LoadSessionAsync(ct).ConfigureAwait(false);
            if (session is null || tokens is null)
            {
                VehiclesCommand.PrintNotLoggedIn("coilpro cmd");
                return ExitError;
            }

            using var clients = CliClientFactory.Create(session);

            try
            {
                CommandResult result;
                if (resolved.Signed is { } signed)
                {
                    var options = new CoilProCommandsOptions
                    {
                        AutoWake = wake,
                        CommandTimeout = timeout,
                        DiagnosticLog = debug ? line => Console.Error.WriteLine($"[debug] {line}") : null,
                    };
                    var commands = new CoilProCommands(clients.ApiHttp, clients.TokenProvider, keyPair!, clients.CoilProClient, options);
                    result = await signed(commands, ct).ConfigureAwait(false);
                }
                else
                {
                    if (wake)
                    {
                        await clients.CoilProClient.WakeUpAsync(vin, ct).ConfigureAwait(false);
                    }

                    var rest = await resolved.Rest!(clients.CoilProClient, ct).ConfigureAwait(false);
                    result = rest.Result ? CommandResult.Succeeded : CommandResult.Refused(rest.Reason);
                }

                if (result.Success)
                {
                    Console.WriteLine("OK");
                    return 0;
                }

                Console.WriteLine($"Refused: {result.Reason ?? "unknown reason"}");
                return ExitRefused;
            }
            catch (TeslaKeyNotPairedException ex)
            {
                Console.Error.WriteLine($"coilpro cmd: {ex.Message}");
                Console.Error.WriteLine("  Open https://tesla.com/_ak/<your registered domain> on a phone with the Tesla app,");
                Console.Error.WriteLine("  confirm the virtual key in the car (key card or existing key), then retry.");
                return ExitKeyNotPaired;
            }
            catch (TeslaVehicleAsleepException ex)
            {
                Console.Error.WriteLine($"coilpro cmd: {ex.Message}");
                Console.Error.WriteLine($"  Wake it first with `coilpro wake {vin}`, or re-run with --wake.");
                return ExitAsleep;
            }
            catch (TeslaCommandException ex)
            {
                var details = new List<string>();
                if (ex.Fault is { } fault && fault != TeslaMessageFault.None)
                {
                    details.Add($"fault: {fault}");
                }

                if (ex.StatusCode is { } status)
                {
                    details.Add($"HTTP {(int)status}");
                }

                var suffix = details.Count > 0 ? $" ({string.Join(", ", details)})" : string.Empty;
                Console.Error.WriteLine($"coilpro cmd: {ex.Message}{suffix}");
                return ExitError;
            }
            catch (TeslaApiException ex)
            {
                Console.Error.WriteLine($"coilpro cmd: {VehiclesCommand.DescribeApiError(ex)}");
                return ExitError;
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine($"coilpro cmd: {ex.Message}");
                return ExitError;
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine($"coilpro cmd: {ex.Message}");
                return ExitError;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                Console.Error.WriteLine("coilpro cmd: cancelled.");
                return 130;
            }
        }
    }

    private static bool IsVin(string value) => value.Length == 17 && value.All(char.IsAsciiLetterOrDigit);

    private static int Usage(string message)
    {
        Console.Error.WriteLine($"coilpro cmd: {message}");
        return ExitError;
    }

    private static void PrintUsage(TextWriter? writer = null)
    {
        writer ??= Console.Out;
        writer.WriteLine(
            """
            coilpro cmd — send a signed vehicle command (Vehicle Command Protocol, no tesla-http-proxy).

            Usage:
              coilpro cmd <VIN> <command> [args] [--wake] [--timeout <seconds>] [--debug]

            Every command is billed as a Fleet API "command" (plus a "wake" with --wake).

            Security and access
              lock | unlock | remote-start | honk | flash | ping
            Closures
              frunk-open | trunk (toggle rear trunk) | trunk-close (powered liftgate)
              auto-secure (Model X) | tonneau-open | tonneau-close | tonneau-stop (Cybertruck)
              windows-vent | windows-close
            Climate
              climate-on | climate-off | temps <driverC> <passengerC>
              defrost-max on|off | bioweapon on|off
              cop on|off [--fan-only] | cop-temp low|medium|high      (cabin overheat protection)
              climate-keeper off|keep|dog|camp
              seat-heater <seat> <0-3>      seat: fl fr rl rlb rc rr rrb 3l 3r (or front-left, ...)
              seat-cooler <left|right> <0-3> | auto-seat <left|right> on|off | wheel-heater on|off
            Charging
              charge-start | charge-stop | charge-limit <percent> | charging-amps <amps>
              charge-port-open | charge-port-close | charge-standard | charge-max
              charge-schedule-add <days> <HH:MM-HH:MM> <lat> <lon> [--id <n>] [--once]
              charge-schedule-remove <id>|home|work|other
              precondition-schedule-add <days> <HH:MM> <lat> <lon> [--id <n>] [--once]
              precondition-schedule-remove <id>|home|work|other
                <days>: mon,tue,... or weekdays|weekend|all. Schedules are added and removed by id; the
                default id is the current Unix time. Schedules need firmware 2024.26+.
              scheduled-charging on|off <HH:MM> | scheduled-departure <HH:MM> [off|all|weekdays] [off|all|weekdays] [HH:MM]
              scheduled-departure-clear                              (legacy; prefer the schedules above)
            Power
              low-power on|off | accessory-power on|off
            Media
              volume <0-10> | volume-up | volume-down | next-track | prev-track | next-fav | prev-fav | play-pause
            Modes, PINs, parental controls
              sentry on|off | guest on|off | erase-guest-data
              valet-on <pin> | valet-off | valet-reset-pin
              pin-to-drive on <pin> | pin-to-drive off | pin-to-drive-reset | pin-to-drive-clear-admin
              speed-limit <mph> | speed-limit-activate <pin> | speed-limit-deactivate <pin>
              speed-limit-clear-pin <pin> | speed-limit-clear-admin
              parental-activate <pin> | parental-deactivate <pin> | parental-speed-limit <mph> | parental-clear-admin
              parental-setting <name> on|off     name: speed-limit acceleration safety-features curfew
                                                 browser-blocked theater-blocked arcade-blocked
            Software, HomeLink, name
              update-schedule <minutes> | update-cancel | homelink <lat> <lon> | rename <name>
            Navigation (unsigned REST commands, no key needed)
              nav <destination text> | nav-gps <lat> <lon> | nav-supercharger <id>

            Options:
              --wake                  Wake a sleeping vehicle first (a billed Fleet API call; off by default)
              --timeout <seconds>     Overall command deadline, default: 30
              --debug                 Print protocol diagnostics to stderr (no keys or payloads)
              --id <n> / --once       Schedule id / run a schedule once (charge- and precondition-schedule-add)
              --fan-only              cop: run the fan only, no A/C

            <VIN> must be the 17-character VIN (see `coilpro vehicles`), not the numeric id.

            Prerequisites: `coilpro keys generate`, partner registration, the virtual key installed in the
            vehicle via https://tesla.com/_ak/<your registered domain>, and `coilpro login` with the
            vehicle_cmds / vehicle_charging_cmds scopes.

            Output and exit codes:
              OK                       exit 0
              Refused: <reason>        exit 2  (vehicle received but refused, e.g. already_set)
              key not paired           exit 3  (install the virtual key)
              vehicle asleep           exit 4  (`coilpro wake <VIN>` or --wake)
              other errors / usage     exit 1
            """);
    }
}
