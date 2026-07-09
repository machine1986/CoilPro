using Coil.Cli.Internal;

namespace Coil.Cli.Commands;

/// <summary>
/// Implements <c>coil vehicles</c>: lists the vehicles on the account tied to the last successful
/// <c>coil login</c>, using the persisted <see cref="CliSession"/> and <see cref="FileTokenStore"/>
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
            Console.Error.WriteLine($"coil vehicles: unexpected argument '{args[0]}'.");
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
            var vehicles = await clients.CoilClient.GetVehiclesAsync(ct).ConfigureAwait(false);

            if (vehicles.Count == 0)
            {
                Console.WriteLine("No vehicles found on this account.");
            }
            else
            {
                PrintTable(vehicles);
            }

            RateLimitPrinter.Print(clients.CoilClient.LastRateLimit);
            return 0;
        }
        catch (TeslaApiException ex)
        {
            Console.Error.WriteLine($"coil vehicles: {DescribeApiError(ex)}");
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"coil vehicles: {ex.Message}");
            return 1;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Console.Error.WriteLine("coil vehicles: cancelled.");
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

    internal static void PrintNotLoggedIn(string command = "coil vehicles") =>
        Console.Error.WriteLine($"{command}: not logged in. Run `coil login` first.");

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
            coil vehicles — list the vehicles on the authenticated Tesla account.

            Usage:
              coil vehicles

            Requires a prior `coil login`; tokens/session are read from ~/.coil.
            """);
    }
}
