using System.Globalization;
using CoilPro.Cli.Internal;
using static CoilPro.Cli.Internal.CliRunner;

namespace CoilPro.Cli.Commands;

/// <summary>
/// The read-only commands added in 0.2: <c>status</c>, <c>cached</c>, <c>alerts</c>, <c>release-notes</c>,
/// <c>service</c>, <c>drivers</c>, <c>nearby</c>, <c>charging-history</c>, <c>invoice</c>, <c>me</c>,
/// <c>region</c>, <c>warranty</c>, <c>options</c>, <c>energy</c> and <c>telemetry</c>. Output is compact text.
/// </summary>
internal static class ReadCommands
{
    private static string VinUsage(string command, string description, string extra = "") =>
        $"""
        coilpro {command} — {description}

        Usage:
          coilpro {command} <VIN>{extra}

        Requires a prior `coilpro login`.
        """;

    private static string? NeedVin(ParsedArgs a) =>
        a.Positional.Count == 1 ? null : "expected exactly one <VIN>.";

    // ---- status -----------------------------------------------------------------------------

    public static Task<int> StatusAsync(string[] args, CancellationToken ct) =>
        RunAsync("status", args,
            VinUsage("status", "key pairing, firmware and protocol requirement (fleet_status; free)."),
            null, null, ct,
            async (a, c) =>
            {
                var vin = a.Positional[0];
                var status = await c.CoilProClient.GetFleetStatusAsync([vin], ct).ConfigureAwait(false);
                var paired = status.KeyPairedVins?.Contains(vin, StringComparer.OrdinalIgnoreCase) == true;
                Console.WriteLine($"VIN:                {vin}");
                Console.WriteLine($"Virtual key paired: {Dash(paired)}");
                if (status.VehicleInfo is { } info && info.FirstOrDefault(kv => string.Equals(kv.Key, vin, StringComparison.OrdinalIgnoreCase)).Value is { } v)
                {
                    Console.WriteLine($"Firmware:           {Dash(v.FirmwareVersion)}");
                    Console.WriteLine($"Protocol required:  {Dash(v.VehicleCommandProtocolRequired)}  (signed commands needed)");
                    Console.WriteLine($"Telemetry version:  {Dash(v.FleetTelemetryVersion)}");
                    Console.WriteLine($"Keys on vehicle:    {Dash(v.TotalNumberOfKeys)}");
                    Console.WriteLine($"Discounted data:    {Dash(v.DiscountedDeviceData)}");
                }

                RateLimitPrinter.Print(c.CoilProClient.LastRateLimit);
                return 0;
            },
            NeedVin);

    // ---- cached -----------------------------------------------------------------------------

    public static Task<int> CachedAsync(string[] args, CancellationToken ct) =>
        RunAsync("cached", args,
            VinUsage("cached", "last vehicle data the car reported (vehicle_data_cached; free, works while asleep, no location)."),
            null, null, ct,
            async (a, c) =>
            {
                var data = await c.CoilProClient.GetVehicleDataCachedAsync(a.Positional[0], ct).ConfigureAwait(false);
                DataCommand.PrintSummary(data);
                RateLimitPrinter.Print(c.CoilProClient.LastRateLimit);
                return 0;
            },
            NeedVin);

    // ---- alerts -----------------------------------------------------------------------------

    public static Task<int> AlertsAsync(string[] args, CancellationToken ct) =>
        RunAsync("alerts", args, VinUsage("alerts", "recent vehicle alerts (free)."), null, null, ct,
            async (a, c) =>
            {
                var alerts = await c.CoilProClient.GetRecentAlertsAsync(a.Positional[0], ct).ConfigureAwait(false);
                if (alerts.Count == 0)
                {
                    Console.WriteLine("No recent alerts.");
                }

                foreach (var alert in alerts)
                {
                    var when = alert.TimeUtc is { } t ? t.ToString("u", CultureInfo.InvariantCulture) : Dash(alert.Time);
                    Console.WriteLine($"{when}  {Dash(alert.Name)}  [{string.Join(',', alert.Audiences ?? [])}]");
                    if (!string.IsNullOrWhiteSpace(alert.UserText))
                    {
                        Console.WriteLine($"    {alert.UserText}");
                    }
                }

                return 0;
            },
            NeedVin);

    // ---- release-notes ----------------------------------------------------------------------

    public static Task<int> ReleaseNotesAsync(string[] args, CancellationToken ct) =>
        RunAsync("release-notes", args,
            VinUsage("release-notes", "release notes of the installed firmware, or the staged update with --staged (free).", " [--staged]"),
            null, ["--staged"], ct,
            async (a, c) =>
            {
                var notes = await c.CoilProClient.GetReleaseNotesAsync(a.Positional[0], a.Flag("--staged"), null, ct).ConfigureAwait(false);
                if (notes.Count == 0)
                {
                    Console.WriteLine(a.Flag("--staged") ? "No staged update." : "No release notes.");
                }

                foreach (var note in notes)
                {
                    Console.WriteLine($"{Dash(note.Title)}{(note.CustomerVersion is null ? string.Empty : $"  (v{note.CustomerVersion})")}");
                    if (!string.IsNullOrWhiteSpace(note.Subtitle))
                    {
                        Console.WriteLine($"  {note.Subtitle}");
                    }

                    if (!string.IsNullOrWhiteSpace(note.Description))
                    {
                        Console.WriteLine($"  {note.Description.ReplaceLineEndings(" ")}");
                    }
                }

                return 0;
            },
            NeedVin);

    // ---- service ----------------------------------------------------------------------------

    public static Task<int> ServiceAsync(string[] args, CancellationToken ct) =>
        RunAsync("service", args, VinUsage("service", "current service status and past service visits (free)."), null, null, ct,
            async (a, c) =>
            {
                var vin = a.Positional[0];
                var service = await c.CoilProClient.GetServiceDataAsync(vin, ct).ConfigureAwait(false);
                Console.WriteLine($"Service status: {Dash(service.ServiceStatus)}");
                if (service.ServiceEtc is { } etc)
                {
                    Console.WriteLine($"Estimated completion: {Dash(etc)}");
                }

                if (!string.IsNullOrEmpty(service.ServiceVisitNumber))
                {
                    Console.WriteLine($"Visit number: {service.ServiceVisitNumber}");
                }

                var history = await c.CoilProClient.GetServiceHistoryAsync(vin, null, 50, ct).ConfigureAwait(false);
                Console.WriteLine();
                Console.WriteLine($"Service history ({history.Items.Count} shown{(history.Count is { } n ? $" of {n}" : string.Empty)}):");
                foreach (var visit in history.Items)
                {
                    Console.WriteLine($"  {Dash(visit.CompletedAt ?? visit.CheckedInAt),-22} {Dash(visit.VisitType),-14} {Dash(visit.Status),-12} {Dash(visit.Location?.Name)}  #{Dash(visit.ServiceVisitNumber)}");
                }

                return 0;
            },
            NeedVin);

    // ---- drivers ----------------------------------------------------------------------------

    public static Task<int> DriversAsync(string[] args, CancellationToken ct) =>
        RunAsync("drivers", args, VinUsage("drivers", "drivers with access to the vehicle (owner only; free)."), null, null, ct,
            async (a, c) =>
            {
                var drivers = await c.CoilProClient.GetDriversAsync(a.Positional[0], ct).ConfigureAwait(false);
                if (drivers.Count == 0)
                {
                    Console.WriteLine("No drivers.");
                }

                foreach (var d in drivers)
                {
                    var name = $"{d.FirstName} {d.LastName}".Trim();
                    Console.WriteLine($"{Dash(d.UserId),-12} {(name.Length == 0 ? "-" : name)}");
                }

                return 0;
            },
            NeedVin);

    // ---- nearby -----------------------------------------------------------------------------

    public static Task<int> NearbyAsync(string[] args, CancellationToken ct) =>
        RunAsync("nearby", args,
            VinUsage("nearby", "Superchargers and destination chargers near the vehicle (BILLED: device data; vehicle must be online).", " [--count <n>] [--radius <miles>]"),
            ["--count", "--radius"], null, ct,
            async (a, c) =>
            {
                var query = new NearbyChargingSitesQuery { Count = a.IntOption("--count"), RadiusMiles = a.IntOption("--radius") };
                var sites = await c.CoilProClient.GetNearbyChargingSitesAsync(a.Positional[0], query, ct).ConfigureAwait(false);
                PrintSites("Superchargers", sites.Superchargers, true);
                PrintSites("Destination chargers", sites.DestinationCharging, false);
                RateLimitPrinter.Print(c.CoilProClient.LastRateLimit);
                return 0;
            },
            NeedVin);

    private static void PrintSites(string title, IReadOnlyList<ChargingSite>? sites, bool stalls)
    {
        Console.WriteLine($"{title}:");
        if (sites is null || sites.Count == 0)
        {
            Console.WriteLine("  (none)");
            return;
        }

        foreach (var s in sites)
        {
            var distance = s.DistanceMiles is { } d ? $"{d:0.#} mi" : "-";
            var stallText = stalls ? $"  {Dash(s.AvailableStalls)}/{Dash(s.TotalStalls)} free" : string.Empty;
            Console.WriteLine($"  {distance,-9} {Dash(s.Name)}{stallText}{(s.SiteClosed == true ? "  CLOSED" : string.Empty)}");
        }
    }

    // ---- charging-history -------------------------------------------------------------------

    public static Task<int> ChargingHistoryAsync(string[] args, CancellationToken ct) =>
        RunAsync("charging-history", args,
            """
            coilpro charging-history — Supercharger charging history (free).

            Usage:
              coilpro charging-history [--vin <VIN>] [--from <date>] [--to <date>] [--page-size <n>]

            Dates: e.g. 2026-11-01 or 2026-11-01T08:00:00Z (UTC when no offset is given).
            Requires a prior `coilpro login`.
            """,
            ["--vin", "--from", "--to", "--page-size"], null, ct,
            async (a, c) =>
            {
                var query = new ChargingHistoryQuery
                {
                    Vin = a.Option("--vin"),
                    StartTime = a.DateOption("--from"),
                    EndTime = a.DateOption("--to"),
                    PageSize = a.IntOption("--page-size"),
                };
                var page = await c.CoilProClient.GetChargingHistoryAsync(query, ct).ConfigureAwait(false);
                if (page.Data.Count == 0)
                {
                    Console.WriteLine("No charging sessions.");
                }

                foreach (var r in page.Data)
                {
                    var total = r.Fees.Sum(f => f.TotalDue ?? 0);
                    var currency = r.Fees.FirstOrDefault()?.CurrencyCode;
                    var invoice = r.Invoices.FirstOrDefault()?.ContentId;
                    Console.WriteLine($"{Dash(r.ChargeStartDateTime),-26} {Dash(r.SiteLocationName)}  {total:0.00} {currency}{(invoice is null ? string.Empty : $"  invoice: {invoice}")}");
                }

                if (page.HasMoreData == true)
                {
                    Console.WriteLine("(more sessions available — narrow with --from/--to or raise --page-size)");
                }

                return 0;
            });

    // ---- invoice ----------------------------------------------------------------------------

    public static Task<int> InvoiceAsync(string[] args, CancellationToken ct) =>
        RunAsync("invoice", args,
            """
            coilpro invoice — download a Supercharger invoice PDF (free).

            Usage:
              coilpro invoice <contentId> [--out <file.pdf>]

            <contentId> is shown by `coilpro charging-history`. Default output: <contentId>.pdf
            in the current directory. Requires a prior `coilpro login`.
            """,
            ["--out"], null, ct,
            async (a, c) =>
            {
                var contentId = a.Positional[0];
                var pdf = await c.CoilProClient.GetChargingInvoiceAsync(contentId, ct).ConfigureAwait(false);
                var path = a.Option("--out") ?? SafeFileName(contentId) + ".pdf";
                await File.WriteAllBytesAsync(path, pdf, ct).ConfigureAwait(false);
                Console.WriteLine($"Saved {pdf.Length} bytes to {path}");
                return 0;
            },
            a => a.Positional.Count == 1 ? null : "expected exactly one <contentId>.");

    private static string SafeFileName(string value) =>
        new(value.Select(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_').ToArray());

    // ---- me / region ------------------------------------------------------------------------

    public static Task<int> MeAsync(string[] args, CancellationToken ct) =>
        RunAsync("me", args, "coilpro me — profile of the authenticated user (needs scope user_data; free).\n\nUsage:\n  coilpro me", null, null, ct,
            async (_, c) =>
            {
                var me = await c.CoilProClient.GetMeAsync(ct).ConfigureAwait(false);
                Console.WriteLine($"Name:  {Dash(me.FullName)}");
                Console.WriteLine($"Email: {Dash(me.Email)}");
                return 0;
            },
            a => a.Positional.Count == 0 ? null : $"unexpected argument '{a.Positional[0]}'.");

    public static Task<int> RegionAsync(string[] args, CancellationToken ct) =>
        RunAsync("region", args, "coilpro region — the user's Fleet API region and base URL (free).\n\nUsage:\n  coilpro region", null, null, ct,
            async (_, c) =>
            {
                var region = await c.CoilProClient.GetRegionAsync(ct).ConfigureAwait(false);
                Console.WriteLine($"Region:   {Dash(region.Region)}");
                Console.WriteLine($"Base URL: {Dash(region.FleetApiBaseUrl)}");
                return 0;
            },
            a => a.Positional.Count == 0 ? null : $"unexpected argument '{a.Positional[0]}'.");

    // ---- warranty / options -----------------------------------------------------------------

    public static Task<int> WarrantyAsync(string[] args, CancellationToken ct) =>
        RunAsync("warranty", args, VinUsage("warranty", "active, upcoming and expired warranties (free)."), null, null, ct,
            async (a, c) =>
            {
                var w = await c.CoilProClient.GetWarrantyDetailsAsync(a.Positional[0], ct).ConfigureAwait(false);
                PrintWarranty("Active", w.ActiveWarranty);
                PrintWarranty("Upcoming", w.UpcomingWarranty);
                PrintWarranty("Expired", w.ExpiredWarranty);
                return 0;
            },
            NeedVin);

    private static void PrintWarranty(string title, IReadOnlyList<WarrantyCoverage>? items)
    {
        Console.WriteLine($"{title}:");
        if (items is null || items.Count == 0)
        {
            Console.WriteLine("  (none)");
            return;
        }

        foreach (var i in items)
        {
            var odo = i.ExpirationOdometer is { } o ? $", {o} {i.OdometerUnit}" : string.Empty;
            Console.WriteLine($"  {Dash(i.WarrantyDisplayName ?? i.WarrantyType)}  until {Dash(i.ExpirationDate ?? i.WarrantyExpiredOn)}{odo}");
        }
    }

    public static Task<int> OptionsAsync(string[] args, CancellationToken ct) =>
        RunAsync("options", args, VinUsage("options", "option codes of the vehicle (free)."), null, null, ct,
            async (a, c) =>
            {
                var options = await c.CoilProClient.GetVehicleOptionsAsync(a.Positional[0], ct).ConfigureAwait(false);
                foreach (var o in options)
                {
                    Console.WriteLine($"{Dash(o.Code),-8} {Dash(o.DisplayName)}");
                }

                return 0;
            },
            NeedVin);

    // ---- energy -----------------------------------------------------------------------------

    private const string EnergyUsage =
        """
        coilpro energy — Powerwall / solar / Wall Connector sites (needs scope energy_device_data).

        Usage:
          coilpro energy products           List all products: vehicles and energy sites
          coilpro energy live <siteId>      Live power flows and battery level of a site

        Requires a prior `coilpro login`. Not yet verified live against real energy hardware.
        """;

    public static Task<int> EnergyAsync(string[] args, CancellationToken ct) =>
        RunAsync("energy", args, EnergyUsage, null, null, ct,
            async (a, c) =>
            {
                switch (a.Positional[0].ToLowerInvariant())
                {
                    case "products":
                        var products = await c.EnergyClient.GetProductsAsync(ct).ConfigureAwait(false);
                        Console.WriteLine($"Vehicles: {products.Vehicles.Count}   Energy sites: {products.EnergySites.Count}");
                        foreach (var s in products.EnergySites)
                        {
                            var battery = s.PercentageCharged is { } p ? $"  battery {p:0.#}%" : string.Empty;
                            Console.WriteLine($"  {s.EnergySiteId,-16} {Dash(s.ResourceType),-16} {Dash(s.SiteName)}{battery}");
                        }

                        break;

                    case "live":
                        if (a.Positional.Count != 2 || !long.TryParse(a.Positional[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var siteId))
                        {
                            throw new CliUsageException("'energy live' needs a numeric <siteId> (see `coilpro energy products`).");
                        }

                        var live = await c.EnergyClient.GetLiveStatusAsync(siteId, ct).ConfigureAwait(false);
                        Console.WriteLine($"Solar:   {Watts(live.SolarPower)}");
                        Console.WriteLine($"Battery: {Watts(live.BatteryPower)}  ({(live.PercentageCharged is { } pc ? $"{pc:0.#}%" : "-")})");
                        Console.WriteLine($"Home:    {Watts(live.LoadPower)}");
                        Console.WriteLine($"Grid:    {Watts(live.GridPower)}  ({Dash(live.GridStatus)})");
                        Console.WriteLine($"Storm Watch active: {Dash(live.StormModeActive)}");
                        break;

                    default:
                        throw new CliUsageException($"unknown subcommand '{a.Positional[0]}' (use products or live).");
                }

                RateLimitPrinter.Print(c.EnergyClient.LastRateLimit);
                return 0;
            },
            a => a.Positional.Count >= 1 ? null : "expected a subcommand: products | live <siteId>.");

    private static string Watts(double? w) => w is { } v ? $"{v:0} W" : "-";

    // ---- telemetry --------------------------------------------------------------------------

    private const string TelemetryUsage =
        """
        coilpro telemetry — read, remove or diagnose a vehicle's Fleet Telemetry configuration (free).

        Usage:
          coilpro telemetry get <VIN>       Show the current configuration and sync state
          coilpro telemetry errors <VIN>    Show recent telemetry errors of the vehicle
          coilpro telemetry delete <VIN>    Remove the configuration

        Creating a configuration needs signing and is done from code (IFleetTelemetryConfigurator);
        the telemetry server itself is not part of CoilPro. Requires a prior `coilpro login`.
        """;

    public static Task<int> TelemetryAsync(string[] args, CancellationToken ct) =>
        RunAsync("telemetry", args, TelemetryUsage, null, null, ct,
            async (a, c) =>
            {
                var vin = a.Positional[1];
                switch (a.Positional[0].ToLowerInvariant())
                {
                    case "get":
                        var status = await c.CoilProClient.GetFleetTelemetryConfigAsync(vin, ct).ConfigureAwait(false);
                        if (status.Config is not { } config)
                        {
                            Console.WriteLine("No telemetry configuration on this vehicle.");
                            Console.WriteLine($"Key paired: {Dash(status.KeyPaired)}");
                            break;
                        }

                        Console.WriteLine($"Synced:   {Dash(status.Synced)}   Key paired: {Dash(status.KeyPaired)}   Limit reached: {Dash(status.LimitReached)}");
                        Console.WriteLine($"Server:   {config.Hostname}:{config.Port}");
                        Console.WriteLine($"Delivery: {Dash(config.DeliveryPolicy)}");
                        Console.WriteLine($"Fields ({config.Fields.Count}):");
                        foreach (var (name, field) in config.Fields.OrderBy(f => f.Key, StringComparer.Ordinal))
                        {
                            Console.WriteLine($"  {name,-28} every {field.IntervalSeconds}s{(field.MinimumDelta is { } d ? $", min delta {d}" : string.Empty)}");
                        }

                        if (config.AlertTypes is { Count: > 0 } alerts)
                        {
                            Console.WriteLine($"Alert types: {string.Join(", ", alerts)}");
                        }

                        break;

                    case "errors":
                        var errors = await c.CoilProClient.GetFleetTelemetryErrorsAsync(vin, ct).ConfigureAwait(false);
                        if (errors.Count == 0)
                        {
                            Console.WriteLine("No telemetry errors.");
                        }

                        foreach (var e in errors)
                        {
                            Console.WriteLine($"{Dash(e.Name)}: {Dash(e.Error)}");
                        }

                        break;

                    case "delete":
                        var updated = await c.CoilProClient.DeleteFleetTelemetryConfigAsync(vin, ct).ConfigureAwait(false);
                        Console.WriteLine($"Telemetry configuration removed ({updated} vehicle(s) updated).");
                        break;

                    default:
                        throw new CliUsageException($"unknown subcommand '{a.Positional[0]}' (use get, errors or delete).");
                }

                return 0;
            },
            a => a.Positional.Count == 2 ? null : "expected: get|errors|delete <VIN>.");
}
