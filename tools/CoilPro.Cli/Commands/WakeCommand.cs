using CoilPro.Cli.Internal;

namespace CoilPro.Cli.Commands;

/// <summary>
/// Implements <c>coilpro wake &lt;vin&gt;</c>: sends a wake-up to a sleeping vehicle so subsequent reads
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
            Console.Error.WriteLine("coilpro wake: missing vehicle id or VIN.");
            Console.Error.WriteLine();
            PrintUsage(Console.Error);
            return 1;
        }

        if (args.Length > 1)
        {
            Console.Error.WriteLine($"coilpro wake: unexpected argument '{args[1]}'.");
            return 1;
        }

        var vehicleTag = args[0];

        var (session, tokens) = await VehiclesCommand.LoadSessionAsync(ct).ConfigureAwait(false);
        if (session is null || tokens is null)
        {
            VehiclesCommand.PrintNotLoggedIn("coilpro wake");
            return 1;
        }

        using var clients = CliClientFactory.Create(session);

        try
        {
            var vehicle = await clients.CoilProClient.WakeUpAsync(vehicleTag, ct).ConfigureAwait(false);

            Console.WriteLine($"Wake-up sent to {(string.IsNullOrEmpty(vehicle.Vin) ? vehicleTag : vehicle.Vin)}.");
            Console.WriteLine($"  State: {vehicle.State ?? "unknown"}");
            if (!string.Equals(vehicle.State, "online", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("  (waking takes a few seconds — re-run `coilpro vehicles` to check)");
            }

            RateLimitPrinter.Print(clients.CoilProClient.LastRateLimit);
            return 0;
        }
        catch (TeslaApiException ex)
        {
            Console.Error.WriteLine($"coilpro wake: {VehiclesCommand.DescribeApiError(ex)}");
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"coilpro wake: {ex.Message}");
            return 1;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Console.Error.WriteLine("coilpro wake: cancelled.");
            return 130;
        }
    }

    private static bool IsHelpFlag(string arg) => arg is "--help" or "-h" or "-?";

    private static void PrintUsage(TextWriter? writer = null)
    {
        writer ??= Console.Out;
        writer.WriteLine(
            """
            coilpro wake — wake a sleeping vehicle so it accepts reads and commands.

            Usage:
              coilpro wake <vin>

            <vin>  Vehicle id or VIN, as shown by `coilpro vehicles`.

            Requires a prior `coilpro login`; tokens/session are read from ~/.coilpro.
            """);
    }
}
