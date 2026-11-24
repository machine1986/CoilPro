using System.Globalization;
using CoilPro.Commands;

namespace CoilPro.Cli.Commands;

/// <summary>Options of <c>coilpro cmd</c> that only some commands use.</summary>
/// <param name="Id">Explicit schedule id (<c>--id</c>); default is the current Unix time in seconds.</param>
/// <param name="Once">Run a schedule once instead of repeating (<c>--once</c>).</param>
/// <param name="FanOnly">Cabin overheat protection: fan only, no A/C (<c>--fan-only</c>).</param>
internal sealed record CmdFlags(ulong? Id, bool Once, bool FanOnly);

/// <summary>A resolved <c>coilpro cmd</c> command: either a signed command or an unsigned REST command.</summary>
internal sealed record ResolvedCommand(
    Func<ICoilProCommands, CancellationToken, Task<CommandResult>>? Signed,
    Func<ICoilProClient, CancellationToken, Task<RestCommandResult>>? Rest);

/// <summary>Maps CLI command names (kebab-case) and arguments to <see cref="ICoilProCommands"/> / REST calls.</summary>
internal static class CmdResolver
{
    public static ResolvedCommand Resolve(string name, string[] a, string vin, CmdFlags f)
    {
        Func<ICoilProCommands, CancellationToken, Task<CommandResult>>? signed = ResolveSigned(name, a, vin, f);
        if (signed is not null)
        {
            return new ResolvedCommand(signed, null);
        }

        var rest = ResolveRest(name, a, vin)
            ?? throw new CliUsageException($"unknown command '{name}'. Run `coilpro cmd --help` for the list.");
        return new ResolvedCommand(null, rest);
    }

    private static Func<ICoilProClient, CancellationToken, Task<RestCommandResult>>? ResolveRest(string name, string[] a, string vin)
    {
        switch (name)
        {
            case "nav":
            {
                if (a.Length == 0)
                {
                    throw new CliUsageException("'nav' needs a destination text, e.g. `coilpro cmd <VIN> nav \"Alexanderplatz, Berlin\"`.");
                }

                var destination = string.Join(' ', a);
                return (c, ct) => c.ShareNavigationDestinationAsync(vin, destination, null, ct);
            }

            case "nav-gps":
            {
                Count(name, a, 2, "<lat> <lon>");
                var (lat, lon) = (Double(a[0], "lat"), Double(a[1], "lon"));
                return (c, ct) => c.NavigateToCoordinatesAsync(vin, lat, lon, NavigationTripOrder.Replace, ct);
            }

            case "nav-supercharger":
            {
                Count(name, a, 1, "<id>");
                if (!long.TryParse(a[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                {
                    throw new CliUsageException($"invalid Supercharger <id> '{a[0]}'.");
                }

                return (c, ct) => c.NavigateToSuperchargerAsync(vin, id, NavigationTripOrder.Replace, ct);
            }

            default:
                return null;
        }
    }

    private static Func<ICoilProCommands, CancellationToken, Task<CommandResult>>? ResolveSigned(string name, string[] a, string vin, CmdFlags f)
    {
        switch (name)
        {
            // ---- security / access ------------------------------------------------------------
            case "lock": return No(name, a, (c, ct) => c.LockAsync(vin, ct));
            case "unlock": return No(name, a, (c, ct) => c.UnlockAsync(vin, ct));
            case "remote-start": return No(name, a, (c, ct) => c.RemoteStartDriveAsync(vin, ct));
            case "honk": return No(name, a, (c, ct) => c.HonkHornAsync(vin, ct));
            case "flash": return No(name, a, (c, ct) => c.FlashLightsAsync(vin, ct));
            case "ping": return No(name, a, (c, ct) => c.PingAsync(vin, ct));

            // ---- closures ---------------------------------------------------------------------
            case "frunk-open": return No(name, a, (c, ct) => c.OpenFrunkAsync(vin, ct));
            case "trunk": return No(name, a, (c, ct) => c.ActuateRearTrunkAsync(vin, ct));
            case "trunk-close": return No(name, a, (c, ct) => c.CloseRearTrunkAsync(vin, ct));
            case "auto-secure": return No(name, a, (c, ct) => c.AutoSecureModelXAsync(vin, ct));
            case "tonneau-open": return No(name, a, (c, ct) => c.OpenTonneauAsync(vin, ct));
            case "tonneau-close": return No(name, a, (c, ct) => c.CloseTonneauAsync(vin, ct));
            case "tonneau-stop": return No(name, a, (c, ct) => c.StopTonneauAsync(vin, ct));
            case "windows-vent": return No(name, a, (c, ct) => c.VentWindowsAsync(vin, ct));
            case "windows-close": return No(name, a, (c, ct) => c.CloseWindowsAsync(vin, ct));

            // ---- climate ----------------------------------------------------------------------
            case "climate-on": return No(name, a, (c, ct) => c.StartClimateAsync(vin, ct));
            case "climate-off": return No(name, a, (c, ct) => c.StopClimateAsync(vin, ct));
            case "temps":
            {
                if (a.Length != 2)
                {
                    throw new CliUsageException("'temps' needs <driverC> <passengerC>, e.g. `coilpro cmd <VIN> temps 21 21`.");
                }

                var driver = Float(a[0], "driverC");
                var passenger = Float(a[1], "passengerC");
                return (c, ct) => c.SetTemperaturesAsync(vin, driver, passenger, ct);
            }

            case "defrost-max":
            {
                var on = OnOff(name, a, 0, 1);
                return (c, ct) => c.SetPreconditioningMaxAsync(vin, on, false, ct);
            }

            case "bioweapon":
            {
                var on = OnOff(name, a, 0, 1);
                return (c, ct) => c.SetBioweaponModeAsync(vin, on, false, ct);
            }

            case "cop":
            {
                var on = OnOff(name, a, 0, 1);
                var fanOnly = f.FanOnly;
                return (c, ct) => c.SetCabinOverheatProtectionAsync(vin, on, fanOnly, ct);
            }

            case "cop-temp":
            {
                Count(name, a, 1, "low|medium|high");
                var temp = Enum(a[0], "temperature", new Dictionary<string, CabinOverheatTemperature>
                {
                    ["low"] = CabinOverheatTemperature.Low,
                    ["medium"] = CabinOverheatTemperature.Medium,
                    ["high"] = CabinOverheatTemperature.High,
                });
                return (c, ct) => c.SetCabinOverheatProtectionTemperatureAsync(vin, temp, ct);
            }

            case "climate-keeper":
            {
                Count(name, a, 1, "off|keep|dog|camp");
                var mode = Enum(a[0], "mode", new Dictionary<string, ClimateKeeperMode>
                {
                    ["off"] = ClimateKeeperMode.Off,
                    ["keep"] = ClimateKeeperMode.Keep,
                    ["dog"] = ClimateKeeperMode.Dog,
                    ["camp"] = ClimateKeeperMode.Camp,
                });
                return (c, ct) => c.SetClimateKeeperModeAsync(vin, mode, false, ct);
            }

            case "seat-heater":
            {
                Count(name, a, 2, "<seat> <0-3>");
                var seat = Enum(a[0], "seat", SeatNames);
                var level = Level<SeatHeaterLevel>(a[1]);
                return (c, ct) => c.SetSeatHeaterAsync(vin, seat, level, ct);
            }

            case "seat-cooler":
            {
                Count(name, a, 2, "<left|right> <0-3>");
                var seat = FrontSeatOf(a[0]);
                var level = Level<SeatCoolerLevel>(a[1]);
                return (c, ct) => c.SetSeatCoolerAsync(vin, seat, level, ct);
            }

            case "auto-seat":
            {
                Count(name, a, 2, "<left|right> on|off");
                var seat = FrontSeatOf(a[0]);
                var on = OnOff(name, a, 1, 2);
                return (c, ct) => c.SetAutoSeatClimateAsync(vin, seat, on, ct);
            }

            case "wheel-heater":
            {
                var on = OnOff(name, a, 0, 1);
                return (c, ct) => c.SetSteeringWheelHeaterAsync(vin, on, ct);
            }

            // ---- charging ---------------------------------------------------------------------
            case "charge-start": return No(name, a, (c, ct) => c.StartChargingAsync(vin, ct));
            case "charge-stop": return No(name, a, (c, ct) => c.StopChargingAsync(vin, ct));
            case "charge-port-open": return No(name, a, (c, ct) => c.OpenChargePortAsync(vin, ct));
            case "charge-port-close": return No(name, a, (c, ct) => c.CloseChargePortAsync(vin, ct));
            case "charge-standard": return No(name, a, (c, ct) => c.ChargeStandardRangeAsync(vin, ct));
            case "charge-max": return No(name, a, (c, ct) => c.ChargeMaxRangeAsync(vin, ct));
            case "charge-limit":
            {
                var percent = Int(a, "charge-limit", "percent");
                return (c, ct) => c.SetChargeLimitAsync(vin, percent, ct);
            }

            case "charging-amps":
            {
                var amps = Int(a, "charging-amps", "amps");
                return (c, ct) => c.SetChargingAmpsAsync(vin, amps, ct);
            }

            case "charge-schedule-add":
            {
                Count(name, a, 4, "<days> <HH:MM-HH:MM> <lat> <lon>");
                var days = Days(a[0]);
                var (start, end) = Window(a[1]);
                var (lat, lon) = (Float(a[2], "lat"), Float(a[3], "lon"));
                var schedule = new ChargeSchedule
                {
                    Id = f.Id ?? NowId(),
                    Days = days,
                    StartTime = start,
                    EndTime = end,
                    OneTime = f.Once,
                    Latitude = lat,
                    Longitude = lon,
                };
                return (c, ct) => c.AddChargeScheduleAsync(vin, schedule, ct);
            }

            case "charge-schedule-remove":
            {
                Count(name, a, 1, "<id>|home|work|other");
                if (TryLocations(a[0], out var locations))
                {
                    return (c, ct) => c.RemoveChargeSchedulesAsync(vin, locations, ct);
                }

                var id = Id(a[0]);
                return (c, ct) => c.RemoveChargeScheduleAsync(vin, id, ct);
            }

            case "precondition-schedule-add":
            {
                Count(name, a, 4, "<days> <HH:MM> <lat> <lon>");
                var days = Days(a[0]);
                var time = Time(a[1]);
                var (lat, lon) = (Float(a[2], "lat"), Float(a[3], "lon"));
                var schedule = new PreconditionSchedule
                {
                    Id = f.Id ?? NowId(),
                    Days = days,
                    PreconditionTime = time,
                    OneTime = f.Once,
                    Latitude = lat,
                    Longitude = lon,
                };
                return (c, ct) => c.AddPreconditionScheduleAsync(vin, schedule, ct);
            }

            case "precondition-schedule-remove":
            {
                Count(name, a, 1, "<id>|home|work|other");
                if (TryLocations(a[0], out var locations))
                {
                    return (c, ct) => c.RemovePreconditionSchedulesAsync(vin, locations, ct);
                }

                var id = Id(a[0]);
                return (c, ct) => c.RemovePreconditionScheduleAsync(vin, id, ct);
            }

            case "scheduled-charging":
            {
                Count(name, a, 2, "on|off <HH:MM>");
                var on = OnOff(name, a, 0, 2);
                var time = Time(a[1]);
                return (c, ct) => c.SetScheduledChargingAsync(vin, on, time, ct);
            }

            case "scheduled-departure":
            {
                if (a.Length is < 1 or > 4)
                {
                    throw new CliUsageException("'scheduled-departure' needs <HH:MM> [precondition off|all|weekdays] [offpeak off|all|weekdays] [offpeakEnd HH:MM].");
                }

                var departure = new ScheduledDeparture
                {
                    DepartureTime = Time(a[0]),
                    Preconditioning = a.Length > 1 ? Policy(a[1]) : DeparturePolicy.Off,
                    OffPeakCharging = a.Length > 2 ? Policy(a[2]) : DeparturePolicy.Off,
                    OffPeakHoursEndTime = a.Length > 3 ? Time(a[3]) : null,
                };
                return (c, ct) => c.SetScheduledDepartureAsync(vin, departure, ct);
            }

            case "scheduled-departure-clear": return No(name, a, (c, ct) => c.ClearScheduledDepartureAsync(vin, ct));

            // ---- power modes ------------------------------------------------------------------
            case "low-power":
            {
                var on = OnOff(name, a, 0, 1);
                return (c, ct) => c.SetLowPowerModeAsync(vin, on, ct);
            }

            case "accessory-power":
            {
                var on = OnOff(name, a, 0, 1);
                return (c, ct) => c.SetKeepAccessoryPowerModeAsync(vin, on, ct);
            }

            // ---- media ------------------------------------------------------------------------
            case "volume":
            {
                Count(name, a, 1, "<0-10>");
                var volume = Float(a[0], "volume");
                return (c, ct) => c.SetVolumeAsync(vin, volume, ct);
            }

            case "volume-up": return No(name, a, (c, ct) => c.VolumeUpAsync(vin, ct));
            case "volume-down": return No(name, a, (c, ct) => c.VolumeDownAsync(vin, ct));
            case "next-track": return No(name, a, (c, ct) => c.NextTrackAsync(vin, ct));
            case "prev-track": return No(name, a, (c, ct) => c.PreviousTrackAsync(vin, ct));
            case "next-fav": return No(name, a, (c, ct) => c.NextFavoriteAsync(vin, ct));
            case "prev-fav": return No(name, a, (c, ct) => c.PreviousFavoriteAsync(vin, ct));
            case "play-pause": return No(name, a, (c, ct) => c.TogglePlaybackAsync(vin, ct));

            // ---- modes and PINs ---------------------------------------------------------------
            case "sentry":
            {
                var on = OnOff(name, a, 0, 1);
                return (c, ct) => c.SetSentryModeAsync(vin, on, ct);
            }

            case "valet-on":
            {
                Count(name, a, 1, "<pin>");
                var pin = a[0];
                return (c, ct) => c.EnableValetModeAsync(vin, pin, ct);
            }

            case "valet-off": return No(name, a, (c, ct) => c.DisableValetModeAsync(vin, ct));
            case "valet-reset-pin": return No(name, a, (c, ct) => c.ResetValetPinAsync(vin, ct));
            case "pin-to-drive":
            {
                if (a.Length is < 1 or > 2)
                {
                    throw new CliUsageException("'pin-to-drive' needs `on <pin>` or `off`.");
                }

                var on = OnOff(name, a, 0, a.Length);
                if (on && a.Length != 2)
                {
                    throw new CliUsageException("'pin-to-drive on' needs a 4-digit <pin>.");
                }

                var pin = a.Length == 2 ? a[1] : null;
                return (c, ct) => c.SetPinToDriveAsync(vin, on, pin, ct);
            }

            case "pin-to-drive-clear-admin": return No(name, a, (c, ct) => c.ClearPinToDriveAdminAsync(vin, ct));
            case "pin-to-drive-reset": return No(name, a, (c, ct) => c.ResetPinToDriveAsync(vin, ct));

            case "speed-limit-activate": return Pin(name, a, (c, pin, ct) => c.ActivateSpeedLimitAsync(vin, pin, ct));
            case "speed-limit-deactivate": return Pin(name, a, (c, pin, ct) => c.DeactivateSpeedLimitAsync(vin, pin, ct));
            case "speed-limit-clear-pin": return Pin(name, a, (c, pin, ct) => c.ClearSpeedLimitPinAsync(vin, pin, ct));
            case "speed-limit-clear-admin": return No(name, a, (c, ct) => c.ClearSpeedLimitPinAdminAsync(vin, ct));
            case "speed-limit":
            {
                Count(name, a, 1, "<mph>");
                var mph = Double(a[0], "mph");
                return (c, ct) => c.SetSpeedLimitAsync(vin, mph, ct);
            }

            case "parental-activate": return Pin(name, a, (c, pin, ct) => c.ActivateParentalControlsAsync(vin, pin, ct));
            case "parental-deactivate": return Pin(name, a, (c, pin, ct) => c.DeactivateParentalControlsAsync(vin, pin, ct));
            case "parental-setting":
            {
                Count(name, a, 2, "<name> on|off");
                var setting = Enum(a[0], "setting", new Dictionary<string, ParentalControlsSetting>
                {
                    ["speed-limit"] = ParentalControlsSetting.SpeedLimit,
                    ["acceleration"] = ParentalControlsSetting.Acceleration,
                    ["safety-features"] = ParentalControlsSetting.SafetyFeatures,
                    ["curfew"] = ParentalControlsSetting.Curfew,
                    ["browser-blocked"] = ParentalControlsSetting.BrowserBlocked,
                    ["theater-blocked"] = ParentalControlsSetting.TheaterBlocked,
                    ["arcade-blocked"] = ParentalControlsSetting.ArcadeBlocked,
                });
                var on = OnOff(name, a, 1, 2);
                return (c, ct) => c.SetParentalControlsSettingAsync(vin, setting, on, ct);
            }

            case "parental-speed-limit":
            {
                Count(name, a, 1, "<mph>");
                var mph = Double(a[0], "mph");
                return (c, ct) => c.SetParentalControlsSpeedLimitAsync(vin, mph, ct);
            }

            case "parental-clear-admin": return No(name, a, (c, ct) => c.ClearParentalControlsPinAdminAsync(vin, ct));

            case "guest":
            {
                var on = OnOff(name, a, 0, 1);
                return (c, ct) => c.SetGuestModeAsync(vin, on, ct);
            }

            case "erase-guest-data": return No(name, a, (c, ct) => c.EraseUserDataAsync(vin, ct));

            // ---- software update, HomeLink, name ----------------------------------------------
            case "update-schedule":
            {
                Count(name, a, 1, "<minutes>");
                if (!double.TryParse(a[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var minutes) || minutes < 0)
                {
                    throw new CliUsageException($"invalid <minutes> '{a[0]}' (expected a number >= 0).");
                }

                var delay = TimeSpan.FromMinutes(minutes);
                return (c, ct) => c.ScheduleSoftwareUpdateAsync(vin, delay, ct);
            }

            case "update-cancel": return No(name, a, (c, ct) => c.CancelSoftwareUpdateAsync(vin, ct));
            case "homelink":
            {
                Count(name, a, 2, "<lat> <lon>");
                var (lat, lon) = (Double(a[0], "lat"), Double(a[1], "lon"));
                return (c, ct) => c.TriggerHomelinkAsync(vin, lat, lon, ct);
            }

            case "rename":
            {
                if (a.Length == 0)
                {
                    throw new CliUsageException("'rename' needs a <name>.");
                }

                var newName = string.Join(' ', a);
                return (c, ct) => c.SetVehicleNameAsync(vin, newName, ct);
            }

            default:
                return null;
        }
    }

    // ---- helpers ------------------------------------------------------------------------------

    private static Func<ICoilProCommands, CancellationToken, Task<CommandResult>> No(
        string name, string[] a, Func<ICoilProCommands, CancellationToken, Task<CommandResult>> call)
    {
        if (a.Length > 0)
        {
            throw new CliUsageException($"'{name}' takes no arguments (got '{a[0]}').");
        }

        return call;
    }

    private static Func<ICoilProCommands, CancellationToken, Task<CommandResult>> Pin(
        string name, string[] a, Func<ICoilProCommands, string, CancellationToken, Task<CommandResult>> call)
    {
        Count(name, a, 1, "<pin>");
        var pin = a[0];
        return (c, ct) => call(c, pin, ct);
    }

    private static void Count(string name, string[] a, int expected, string usage)
    {
        if (a.Length != expected)
        {
            throw new CliUsageException($"'{name}' needs {usage}.");
        }
    }

    private static bool OnOff(string name, string[] a, int index, int expectedLength)
    {
        if (a.Length < expectedLength || a.Length <= index)
        {
            throw new CliUsageException($"'{name}' needs on|off.");
        }

        return a[index].ToLowerInvariant() switch
        {
            "on" or "true" or "1" => true,
            "off" or "false" or "0" => false,
            _ => throw new CliUsageException($"'{name}': expected on|off, got '{a[index]}'."),
        };
    }

    private static T Enum<T>(string text, string label, IReadOnlyDictionary<string, T> map)
    {
        if (map.TryGetValue(text.ToLowerInvariant(), out var value))
        {
            return value;
        }

        throw new CliUsageException($"invalid {label} '{text}' (use {string.Join('|', map.Keys.Distinct())}).");
    }

    private static readonly Dictionary<string, SeatPosition> SeatNames = new()
    {
        ["fl"] = SeatPosition.FrontLeft, ["front-left"] = SeatPosition.FrontLeft,
        ["fr"] = SeatPosition.FrontRight, ["front-right"] = SeatPosition.FrontRight,
        ["rl"] = SeatPosition.RearLeft, ["rear-left"] = SeatPosition.RearLeft,
        ["rlb"] = SeatPosition.RearLeftBack, ["rear-left-back"] = SeatPosition.RearLeftBack,
        ["rc"] = SeatPosition.RearCenter, ["rear-center"] = SeatPosition.RearCenter,
        ["rr"] = SeatPosition.RearRight, ["rear-right"] = SeatPosition.RearRight,
        ["rrb"] = SeatPosition.RearRightBack, ["rear-right-back"] = SeatPosition.RearRightBack,
        ["3l"] = SeatPosition.ThirdRowLeft, ["third-left"] = SeatPosition.ThirdRowLeft,
        ["3r"] = SeatPosition.ThirdRowRight, ["third-right"] = SeatPosition.ThirdRowRight,
    };

    private static FrontSeat FrontSeatOf(string text) => Enum(text, "seat", new Dictionary<string, FrontSeat>
    {
        ["left"] = FrontSeat.FrontLeft,
        ["right"] = FrontSeat.FrontRight,
    });

    /// <summary>Parses a level given as 0-3 or off|low|medium|high into an enum with those four members.</summary>
    private static T Level<T>(string text) where T : struct, System.Enum
    {
        var levels = new Dictionary<string, int> { ["0"] = 0, ["off"] = 0, ["1"] = 1, ["low"] = 1, ["2"] = 2, ["medium"] = 2, ["3"] = 3, ["high"] = 3 };
        var index = Enum(text, "level", levels);
        return (T)System.Enum.ToObject(typeof(T), index);
    }

    private static DeparturePolicy Policy(string text) => Enum(text, "policy", new Dictionary<string, DeparturePolicy>
    {
        ["off"] = DeparturePolicy.Off,
        ["all"] = DeparturePolicy.AllWeek,
        ["weekdays"] = DeparturePolicy.WeekdaysOnly,
    });

    private static ScheduleDays Days(string text)
    {
        var result = ScheduleDays.None;
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            result |= part.ToLowerInvariant() switch
            {
                "all" or "daily" or "everyday" => ScheduleDays.All,
                "weekdays" => ScheduleDays.Weekdays,
                "weekend" => ScheduleDays.Saturday | ScheduleDays.Sunday,
                "mon" or "monday" => ScheduleDays.Monday,
                "tue" or "tuesday" => ScheduleDays.Tuesday,
                "wed" or "wednesday" => ScheduleDays.Wednesday,
                "thu" or "thursday" => ScheduleDays.Thursday,
                "fri" or "friday" => ScheduleDays.Friday,
                "sat" or "saturday" => ScheduleDays.Saturday,
                "sun" or "sunday" => ScheduleDays.Sunday,
                _ => throw new CliUsageException($"invalid day '{part}' (use mon,tue,... or weekdays|weekend|all)."),
            };
        }

        return result == ScheduleDays.None ? throw new CliUsageException("<days> must not be empty.") : result;
    }

    private static TimeOnly Time(string text) =>
        TimeOnly.TryParseExact(text, ["H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var t)
            ? t
            : throw new CliUsageException($"invalid time '{text}' (expected HH:MM, 24 hours).");

    private static (TimeOnly Start, TimeOnly End) Window(string text)
    {
        var parts = text.Split('-');
        return parts.Length == 2
            ? (Time(parts[0]), Time(parts[1]))
            : throw new CliUsageException($"invalid time window '{text}' (expected HH:MM-HH:MM).");
    }

    private static bool TryLocations(string text, out ScheduleLocations locations)
    {
        locations = ScheduleLocations.None;
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (part.ToLowerInvariant())
            {
                case "home": locations |= ScheduleLocations.Home; break;
                case "work": locations |= ScheduleLocations.Work; break;
                case "other": locations |= ScheduleLocations.Other; break;
                default:
                    locations = ScheduleLocations.None;
                    return false;
            }
        }

        return locations != ScheduleLocations.None;
    }

    private static ulong Id(string text) =>
        ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : throw new CliUsageException($"invalid schedule id '{text}' (expected a number, or home|work|other).");

    private static ulong NowId() => (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    private static int Int(string[] a, string name, string label)
    {
        if (a.Length != 1 || !int.TryParse(a[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new CliUsageException($"'{name}' needs one integer <{label}>.");
        }

        return value;
    }

    private static float Float(string text, string label) =>
        float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new CliUsageException($"invalid <{label}> '{text}' (use '.' as decimal separator).");

    private static double Double(string text, string label) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new CliUsageException($"invalid <{label}> '{text}' (use '.' as decimal separator).");
}
