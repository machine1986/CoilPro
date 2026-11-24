using System.Globalization;
using Coil.Cli.Internal;
using Coil.Commands;

namespace Coil.Cli.Commands;

/// <summary>
/// Implements <c>coil cmd &lt;VIN&gt; &lt;command&gt; [args]</c>: sends one signed vehicle command via the
/// Vehicle Command Protocol (<see cref="CoilCommands"/>).
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
            Console.Error.WriteLine("coil cmd: expected <VIN> <command>.");
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
                    ? $"coil cmd: '{vin}' looks like the numeric vehicle id. Signed commands need the 17-character VIN — see `coil vehicles`."
                    : $"coil cmd: '{vin}' is not a valid VIN (expected 17 characters). See `coil vehicles`.");
            return ExitError;
        }

        vin = vin.ToUpperInvariant();

        Func<ICoilCommands, CancellationToken, Task<CommandResult>> action;
        try
        {
            action = ResolveCommand(name, cmdArgs, vin);
        }
        catch (CliUsageException ex)
        {
            return Usage(ex.Message);
        }

        TeslaKeyPair keyPair;
        try
        {
            if (!File.Exists(CoilPaths.PrivateKeyFile))
            {
                Console.Error.WriteLine($"coil cmd: no private key at {CoilPaths.PrivateKeyFile}. Run `coil keys generate` first.");
                return ExitError;
            }

            keyPair = TeslaKeyPair.FromPrivateKeyPem(File.ReadAllText(CoilPaths.PrivateKeyFile));
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"coil cmd: could not read the private key: {ex.Message}");
            return ExitError;
        }

        using (keyPair)
        {
            var (session, tokens) = await VehiclesCommand.LoadSessionAsync(ct).ConfigureAwait(false);
            if (session is null || tokens is null)
            {
                VehiclesCommand.PrintNotLoggedIn("coil cmd");
                return ExitError;
            }

            using var clients = CliClientFactory.Create(session);

            var options = new CoilCommandsOptions
            {
                AutoWake = wake,
                CommandTimeout = timeout,
                DiagnosticLog = debug ? line => Console.Error.WriteLine($"[debug] {line}") : null,
            };
            var commands = new CoilCommands(clients.ApiHttp, clients.TokenProvider, keyPair, clients.CoilClient, options);

            try
            {
                var result = await action(commands, ct).ConfigureAwait(false);
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
                Console.Error.WriteLine($"coil cmd: {ex.Message}");
                Console.Error.WriteLine("  Open https://tesla.com/_ak/<your registered domain> on a phone with the Tesla app,");
                Console.Error.WriteLine("  confirm the virtual key in the car (key card or existing key), then retry.");
                return ExitKeyNotPaired;
            }
            catch (TeslaVehicleAsleepException ex)
            {
                Console.Error.WriteLine($"coil cmd: {ex.Message}");
                Console.Error.WriteLine($"  Wake it first with `coil wake {vin}`, or re-run with --wake.");
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
                Console.Error.WriteLine($"coil cmd: {ex.Message}{suffix}");
                return ExitError;
            }
            catch (TeslaApiException ex)
            {
                Console.Error.WriteLine($"coil cmd: {VehiclesCommand.DescribeApiError(ex)}");
                return ExitError;
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine($"coil cmd: {ex.Message}");
                return ExitError;
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine($"coil cmd: {ex.Message}");
                return ExitError;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                Console.Error.WriteLine("coil cmd: cancelled.");
                return 130;
            }
        }
    }

    private static Func<ICoilCommands, CancellationToken, Task<CommandResult>> ResolveCommand(string name, string[] a, string vin)
    {
        switch (name)
        {
            case "lock": NoArgs(name, a); return (c, ct) => c.LockAsync(vin, ct);
            case "unlock": NoArgs(name, a); return (c, ct) => c.UnlockAsync(vin, ct);
            case "honk": NoArgs(name, a); return (c, ct) => c.HonkHornAsync(vin, ct);
            case "flash": NoArgs(name, a); return (c, ct) => c.FlashLightsAsync(vin, ct);
            case "climate-on": NoArgs(name, a); return (c, ct) => c.StartClimateAsync(vin, ct);
            case "climate-off": NoArgs(name, a); return (c, ct) => c.StopClimateAsync(vin, ct);
            case "charge-start": NoArgs(name, a); return (c, ct) => c.StartChargingAsync(vin, ct);
            case "charge-stop": NoArgs(name, a); return (c, ct) => c.StopChargingAsync(vin, ct);
            case "charge-port-open": NoArgs(name, a); return (c, ct) => c.OpenChargePortAsync(vin, ct);
            case "temps":
            {
                if (a.Length != 2)
                {
                    throw new CliUsageException("'temps' needs <driverC> <passengerC>, e.g. `coil cmd <VIN> temps 21 21`.");
                }

                var driver = ParseFloat(a[0], "driverC");
                var passenger = ParseFloat(a[1], "passengerC");
                return (c, ct) => c.SetTemperaturesAsync(vin, driver, passenger, ct);
            }

            case "charge-limit":
            {
                var percent = ParseInt(a, "charge-limit", "percent");
                return (c, ct) => c.SetChargeLimitAsync(vin, percent, ct);
            }

            case "charging-amps":
            {
                var amps = ParseInt(a, "charging-amps", "amps");
                return (c, ct) => c.SetChargingAmpsAsync(vin, amps, ct);
            }

            default:
                throw new CliUsageException($"unknown command '{name}'. Run `coil cmd --help` for the list.");
        }
    }

    private static void NoArgs(string name, string[] a)
    {
        if (a.Length > 0)
        {
            throw new CliUsageException($"'{name}' takes no arguments (got '{a[0]}').");
        }
    }

    private static int ParseInt(string[] a, string name, string label)
    {
        if (a.Length != 1 || !int.TryParse(a[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new CliUsageException($"'{name}' needs one integer <{label}>.");
        }

        return value;
    }

    private static float ParseFloat(string text, string label)
    {
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw new CliUsageException($"invalid <{label}> '{text}' (use '.' as decimal separator).");
        }

        return value;
    }

    private static bool IsVin(string value) => value.Length == 17 && value.All(char.IsAsciiLetterOrDigit);

    private static int Usage(string message)
    {
        Console.Error.WriteLine($"coil cmd: {message}");
        return ExitError;
    }

    private static void PrintUsage(TextWriter? writer = null)
    {
        writer ??= Console.Out;
        writer.WriteLine(
            """
            coil cmd — send a signed vehicle command (Vehicle Command Protocol, no tesla-http-proxy).

            Usage:
              coil cmd <VIN> <command> [args] [--wake] [--timeout <seconds>] [--debug]

            Commands:
              lock | unlock | honk | flash
              climate-on | climate-off
              temps <driverC> <passengerC>
              charge-start | charge-stop
              charge-limit <percent>        (50-100)
              charge-port-open
              charging-amps <amps>

            Options:
              --wake                  Wake a sleeping vehicle first (a billed Fleet API call; off by default)
              --timeout <seconds>     Overall command deadline, default: 30
              --debug                 Print protocol diagnostics to stderr (no keys or payloads)

            <VIN> must be the 17-character VIN (see `coil vehicles`), not the numeric id.

            Prerequisites: `coil keys generate`, partner registration, the virtual key installed in the
            vehicle via https://tesla.com/_ak/<your registered domain>, and `coil login` with the
            vehicle_cmds / vehicle_charging_cmds scopes.

            Output and exit codes:
              OK                       exit 0
              Refused: <reason>        exit 2  (vehicle received but refused, e.g. already_set)
              key not paired           exit 3  (install the virtual key)
              vehicle asleep           exit 4  (`coil wake <VIN>` or --wake)
              other errors / usage     exit 1
            """);
    }
}
