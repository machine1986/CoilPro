using CoilPro.Cli.Internal;

namespace CoilPro.Cli.Commands;

/// <summary>
/// Implements <c>coilpro vehicles</c>: lists the vehicles on the account tied to the last successful
/// <c>coilpro login</c>, using the persisted <see cref="CliSession"/> and <see cref="FileTokenStore"/>
/// (refreshing the access token transparently via <see cref="TeslaTokenProvider"/> if needed).
/// </summary>
internal static class VehiclesCommand
{
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        if (args.Any(IsHelpFlag))
        {
            PrintUsage();
            return 0;
        }

        if (args.Length > 0)
        {
            Console.Error.WriteLine($"coilpro vehicles: unexpected argument '{args[0]}'.");
            return 1;
        }

        var (session, tokens) = await LoadSessionAsync(ct).ConfigureAwait(false);
        if (session is null || tokens is null)
        {
            PrintNotLoggedIn();
            return 1;
        }

        using var clients = CliClientFactory.Create(session);

        try
        {
            var vehicles = await clients.CoilProClient.GetVehiclesAsync(ct).ConfigureAwait(false);

            if (vehicles.Count == 0)
            {
                Console.WriteLine("No vehicles found on this account.");
            }
            else
            {
                PrintTable(vehicles);
            }

            RateLimitPrinter.Print(clients.CoilProClient.LastRateLimit);
            return 0;
        }
        catch (TeslaApiException ex)
        {
            Console.Error.WriteLine($"coilpro vehicles: {DescribeApiError(ex)}");
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"coilpro vehicles: {ex.Message}");
            return 1;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Console.Error.WriteLine("coilpro vehicles: cancelled.");
            return 130;
        }
    }

    internal static async Task<(CliSession? Session, TeslaTokens? Tokens)> LoadSessionAsync(CancellationToken ct)
    {
        var session = await CliSession.LoadAsync(ct: ct).ConfigureAwait(false);
        if (session is null)
        {
            return (null, null);
        }

        var tokenStore = new FileTokenStore();
        var tokens = await tokenStore.GetAsync(ct).ConfigureAwait(false);
        return tokens is null ? (null, null) : (session, tokens);
    }

    internal static string DescribeApiError(TeslaApiException ex) =>
        $"Tesla Fleet API returned {(int)ex.StatusCode} {ex.StatusCode}" +
        (ex.ErrorCode is null ? string.Empty : $" ({ex.ErrorCode})") +
        $": {ex.Message}";

    internal static void PrintNotLoggedIn(string command = "coilpro vehicles") =>
        Console.Error.WriteLine($"{command}: not logged in. Run `coilpro login` first.");

    private static void PrintTable(IReadOnlyList<Vehicle> vehicles)
    {
        Console.WriteLine($"{"VIN",-18} {"STATE",-9} {"ID",-14} DISPLAY NAME");
        foreach (var vehicle in vehicles)
        {
            Console.WriteLine($"{vehicle.Vin,-18} {vehicle.State ?? "-",-9} {vehicle.Id,-14} {vehicle.DisplayName ?? "-"}");
        }
    }

    private static bool IsHelpFlag(string arg) => arg is "--help" or "-h" or "-?";

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            coilpro vehicles — list the vehicles on the authenticated Tesla account.

            Usage:
              coilpro vehicles

            Requires a prior `coilpro login`; tokens/session are read from ~/.coilpro.
            """);
    }
}
