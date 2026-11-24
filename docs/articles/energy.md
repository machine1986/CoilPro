# Energy Products

`ICoilProEnergyClient` talks to Tesla energy products on the Fleet API: Powerwall, solar and Wall Connector sites. Site ids come from `GetProductsAsync`.

> **Unofficial — not affiliated with Tesla, Inc.**

> **Status (0.2.0).** Not yet verified against live energy hardware. The test account has Wall Connector sites, and `products` answered correctly (`resource_type: wall_connector`), but the energy-specific endpoints answered `403 Unauthorized missing scopes` because the user token had no `energy_device_data` scope at the time. Response shapes follow Tesla's documentation and are covered by tests; treat them as unconfirmed until noted in [API deviations](../api-deviations.md).

## Scopes

| Scope | `TeslaScopes` | Needed for |
|---|---|---|
| `energy_device_data` | `EnergyDeviceData` | all reads |
| `energy_cmds` | `EnergyCmds` | all settings |

Request them at login and enable them for your application on developer.tesla.com. The `coilpro login` default scopes include both.

## Setup

```csharp
using CoilPro;

using var apiHttp = new HttpClient { BaseAddress = TeslaEndpoints.For(TeslaRegion.Europe).FleetApiBaseUrl };
ICoilProEnergyClient energy = new CoilProEnergyClient(apiHttp, tokenProvider);
```

With ASP.NET Core, `AddCoilProApi` registers `ICoilProEnergyClient` next to `ICoilProClient`, using the same `HttpClient`, token provider and options.

## Reading

```csharp
EnergyProducts products = await energy.GetProductsAsync();
foreach (var site in products.EnergySites)
    Console.WriteLine($"{site.EnergySiteId} {site.ResourceType} {site.SiteName}");

EnergyLiveStatus live = await energy.GetLiveStatusAsync(siteId);
Console.WriteLine($"Solar {live.SolarPower} W, battery {live.PercentageCharged}%, grid {live.GridPower} W");
```

Power values are in watts, energy in watt-hours.

| Method | Endpoint | Returns |
|---|---|---|
| `GetProductsAsync` | `GET products` | Vehicles and energy sites of the account |
| `GetLiveStatusAsync` | `GET energy_sites/{id}/live_status` | Power flows, battery level, grid status |
| `GetSiteInfoAsync` | `GET …/site_info` | Configuration and components |
| `GetSiteStatusAsync` | `GET …/site_status` | Summary status |
| `GetBackupTimeRemainingAsync` | `GET …/backup_time_remaining` | Hours of backup left |
| `GetWallConnectorLiveStatusAsync` | `GET …/charger_live_status` | State and power per Wall Connector |
| `GetTariffRateAsync` | `GET …/tariff_rate` | Utility tariff; `null` when the site has none (for example Tesla Electric) |
| `GetBackupHistoryAsync` | `GET …/calendar_history?kind=backup` | Grid outage events |
| `GetEnergyHistoryAsync` | `GET …/calendar_history?kind=energy` | Energy flows over time (Wh) |
| `GetChargeHistoryAsync` | `GET …/telemetry_history?kind=charge` | Wall Connector charge sessions |

History calls take an `EnergyHistoryQuery` (start, end, `EnergyHistoryPeriod`, IANA time zone).

## Settings

| Method | Endpoint | Effect |
|---|---|---|
| `SetBackupReserveAsync` | `POST …/backup` | Battery percentage reserved for backup |
| `SetOffGridVehicleChargingReserveAsync` | `POST …/off_grid_vehicle_charging_reserve` | Reserve for charging vehicles off grid |
| `SetOperationModeAsync` | `POST …/operation` | `EnergyOperationMode` (`SelfConsumption` or `Autonomous`) |
| `SetStormModeAsync` | `POST …/storm_mode` | Storm Watch on or off |
| `SetGridImportExportAsync` | `POST …/grid_import_export` | Grid charging with solar and `GridExportRule` (`BatteryOk`, `PvOnly`, `Never`); `null` leaves a setting unchanged |
| `SetTimeOfUseSettingsAsync` | `POST …/time_of_use_settings` | Replaces the time-of-use tariff (Tesla's tariff JSON) |

Settings change real hardware. Try them on a test site first.

## CLI

```bash
coilpro energy products
coilpro energy live <siteId>
```

## Errors and limits

Failures throw `TeslaApiException`; `LastRateLimit` exposes the latest rate-limit snapshot, as on `ICoilProClient`.
