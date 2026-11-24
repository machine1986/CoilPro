using CoilPro.Cli.Internal;

namespace CoilPro.Cli.Commands;

/// <summary>
/// Implements <c>coilpro data &lt;vin&gt;</c>: fetches a snapshot of vehicle data for one vehicle on the
/// account tied to the last successful <c>coilpro login</c>, and prints a concise human-readable summary.
/// </summary>
internal static class DataCommand
{
    /// <summary>Kept small by default to minimize Fleet API cost (it's pay-per-use).</summary>
    private const VehicleDataEndpoint DefaultEndpoints =
        VehicleDataEndpoint.ChargeState | VehicleDataEndpoint.DriveState | VehicleDataEndpoint.ClimateState | VehicleDataEndpoint.VehicleState;

    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        string? vin = null;
        var endpoints = VehicleDataEndpoint.None;

        var i = 0;
        while (i < args.Length)
        {
            var arg = args[i];

            if (IsHelpFlag(arg))
            {
                PrintUsage();
                return 0;
            }

            var (name, inlineValue) = SplitFlag(arg);

            if (name == "--endpoints")
            {
                string value;
                if (inlineValue is not null)
                {
                    value = inlineValue;
                }
                else if (i + 1 < args.Length)
                {
                    i++;
                    value = args[i];
                }
                else
                {
                    Console.Error.WriteLine("coilpro data: missing value for --endpoints.");
                    return 1;
                }

                try
                {
                    endpoints |= ParseEndpoints(value);
                }
                catch (CliUsageException ex)
                {
                    Console.Error.WriteLine($"coilpro data: {ex.Message}");
                    return 1;
                }
            }
            else if (arg.StartsWith('-'))
            {
                Console.Error.WriteLine($"coilpro data: unknown option '{arg}'.");
                return 1;
            }
            else if (vin is null)
            {
                vin = arg;
            }
            else
            {
                Console.Error.WriteLine($"coilpro data: unexpected argument '{arg}'.");
                return 1;
            }

            i++;
        }

        if (string.IsNullOrWhiteSpace(vin))
        {
            Console.Error.WriteLine("coilpro data: missing <vin>. Usage: coilpro data <vin> [--endpoints e1,e2,...]");
            return 1;
        }

        if (endpoints == VehicleDataEndpoint.None)
        {
            endpoints = DefaultEndpoints;
        }

        var (session, tokens) = await VehiclesCommand.LoadSessionAsync(ct).ConfigureAwait(false);
        if (session is null || tokens is null)
        {
            VehiclesCommand.PrintNotLoggedIn("coilpro data");
            return 1;
        }

        using var clients = CliClientFactory.Create(session);

        try
        {
            var data = await clients.CoilProClient.GetVehicleDataAsync(vin, endpoints, ct).ConfigureAwait(false);
            PrintSummary(data);
            RateLimitPrinter.Print(clients.CoilProClient.LastRateLimit);
            return 0;
        }
        catch (TeslaApiException ex)
        {
            Console.Error.WriteLine($"coilpro data: {VehiclesCommand.DescribeApiError(ex)}");
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"coilpro data: {ex.Message}");
            return 1;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Console.Error.WriteLine("coilpro data: cancelled.");
            return 130;
        }
    }

    internal static void PrintSummary(VehicleData data)
    {
        Console.WriteLine($"Vehicle: {data.Vin ?? "-"} (id {data.Id})");
        Console.WriteLine($"State:   {data.State ?? "-"}");
        Console.WriteLine();

        if (data.ChargeState is { } charge)
        {
            Console.WriteLine("Battery:");
            Console.WriteLine($"  Level:          {FormatPercent(charge.BatteryLevel)} (usable: {FormatPercent(charge.UsableBatteryLevel)})");
            Console.WriteLine($"  Range:          {FormatNumber(charge.BatteryRange)}");
            Console.WriteLine($"  Charging state: {charge.ChargingState ?? "-"}");
            if (charge.MinutesToFullCharge is > 0)
            {
                Console.WriteLine($"  Time to full:   {charge.MinutesToFullCharge} min");
            }

            Console.WriteLine();
        }

        if (data.VehicleState is { } vehicleState)
        {
            Console.WriteLine("Vehicle:");
            Console.WriteLine($"  Odometer:  {FormatNumber(vehicleState.Odometer)}");
            Console.WriteLine($"  Locked:    {FormatBool(vehicleState.Locked)}");
            Console.WriteLine($"  Sw. ver.:  {vehicleState.CarVersion ?? "-"}");
            Console.WriteLine();
        }

        if (data.ClimateState is { } climate)
        {
            Console.WriteLine("Climate:");
            Console.WriteLine($"  Climate on:   {FormatBool(climate.IsClimateOn)}");
            Console.WriteLine($"  Inside temp:  {FormatNumber(climate.InsideTemp)}");
            Console.WriteLine($"  Outside temp: {FormatNumber(climate.OutsideTemp)}");
            Console.WriteLine();
        }

        if (data.DriveState is { } drive)
        {
            Console.WriteLine("Drive:");
            Console.WriteLine($"  Shift state: {drive.ShiftState ?? "-"}");
            Console.WriteLine($"  Speed:       {FormatNumber(drive.Speed)}");
            Console.WriteLine();
        }

        Console.WriteLine("(distance/speed/temperature values are as returned by the Fleet API — units follow the vehicle's own gui_settings, not necessarily your locale.)");
    }

    private static string FormatPercent(int? value) => value is { } v ? $"{v}%" : "-";

    private static string FormatNumber(double? value) => value is { } v ? $"{v:0.#}" : "-";

    private static string FormatBool(bool? value) => value switch
    {
        true => "yes",
        false => "no",
        null => "-",
    };

    private static VehicleDataEndpoint ParseEndpoints(string value)
    {
        var result = VehicleDataEndpoint.None;

        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            result |= part.Replace("_", string.Empty).ToLowerInvariant() switch
            {
                "chargestate" => VehicleDataEndpoint.ChargeState,
                "climatestate" => VehicleDataEndpoint.ClimateState,
                "drivestate" => VehicleDataEndpoint.DriveState,
                "guisettings" => VehicleDataEndpoint.GuiSettings,
                "vehiclestate" => VehicleDataEndpoint.VehicleState,
                "vehicleconfig" => VehicleDataEndpoint.VehicleConfig,
                "locationdata" => VehicleDataEndpoint.LocationData,
                "closuresstate" => VehicleDataEndpoint.ClosuresState,
                _ => throw new CliUsageException($"Unknown endpoint: '{part}'."),
            };
        }

        return result;
    }

    private static (string Name, string? InlineValue) SplitFlag(string arg)
    {
        var eq = arg.IndexOf('=');
        return eq < 0 ? (arg, null) : (arg[..eq], arg[(eq + 1)..]);
    }

    private static bool IsHelpFlag(string arg) => arg is "--help" or "-h" or "-?";

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            coilpro data <vin> — fetch a snapshot of vehicle data (battery, drive, climate, vehicle state).

            Usage:
              coilpro data <vin> [--endpoints <e1,e2,...>]

            Options:
              --endpoints <list>   Comma-separated data segments to request, e.g.
                                    charge_state,drive_state,climate_state,vehicle_state
                                    (default: charge_state,drive_state,climate_state,vehicle_state)

            Requires a prior `coilpro login`. The vehicle must be awake to return vehicle_data —
            use `coilpro vehicles` to check its state first (waking commands land with the Vehicle
            Command Protocol milestone).
            """);
    }
}
