using Coil.Cli.Internal;

namespace Coil.Cli.Commands;

/// <summary>
/// Implements <c>coil wake &lt;vin&gt;</c>: sends a wake-up to a sleeping vehicle so subsequent reads
/// and commands reach it.
/// </summary>
/// <remarks>
/// Waking is asynchronous on Tesla's side — a successful call means the request was accepted, not
/// that the vehicle is already online. The reported state is a snapshot at response time.
/// </remarks>
internal static class WakeCommand
{
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        if (args.Any(IsHelpFlag))
        {
            PrintUsage();
            return 0;
        }

        if (args.Length == 0)
        {
            Console.Error.WriteLine("coil wake: missing vehicle id or VIN.");
            Console.Error.WriteLine();
            PrintUsage(Console.Error);
            return 1;
        }

        if (args.Length > 1)
        {
            Console.Error.WriteLine($"coil wake: unexpected argument '{args[1]}'.");
            return 1;
        }

        var vehicleTag = args[0];

        var (session, tokens) = await VehiclesCommand.LoadSessionAsync(ct).ConfigureAwait(false);
        if (session is null || tokens is null)
        {
            VehiclesCommand.PrintNotLoggedIn("coil wake");
            return 1;
        }

        using var clients = CliClientFactory.Create(session);

        try
        {
            var vehicle = await clients.CoilClient.WakeUpAsync(vehicleTag, ct).ConfigureAwait(false);

            Console.WriteLine($"Wake-up sent to {(string.IsNullOrEmpty(vehicle.Vin) ? vehicleTag : vehicle.Vin)}.");
            Console.WriteLine($"  State: {vehicle.State ?? "unknown"}");
            if (!string.Equals(vehicle.State, "online", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("  (waking takes a few seconds — re-run `coil vehicles` to check)");
            }

            RateLimitPrinter.Print(clients.CoilClient.LastRateLimit);
            return 0;
        }
        catch (TeslaApiException ex)
        {
            Console.Error.WriteLine($"coil wake: {VehiclesCommand.DescribeApiError(ex)}");
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"coil wake: {ex.Message}");
            return 1;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Console.Error.WriteLine("coil wake: cancelled.");
            return 130;
        }
    }

    private static bool IsHelpFlag(string arg) => arg is "--help" or "-h" or "-?";

    private static void PrintUsage(TextWriter? writer = null)
    {
        writer ??= Console.Out;
        writer.WriteLine(
            """
            coil wake — wake a sleeping vehicle so it accepts reads and commands.

            Usage:
              coil wake <vin>

            <vin>  Vehicle id or VIN, as shown by `coil vehicles`.

            Requires a prior `coil login`; tokens/session are read from ~/.coil.
            """);
    }
}
