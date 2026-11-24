# Signed Vehicle Commands

`ICoilProCommands` (package `CoilPro.Commands`) sends commands such as lock, flash lights or set charge limit to the vehicle using Tesla's **Vehicle Command Protocol**, implemented natively in C#. Commands are signed with your P-256 key and sent through the Fleet API's `signed_command` endpoint — no `tesla-http-proxy` sidecar required.

> **Unofficial — not affiliated with Tesla, Inc.**

> **Status (0.2.0).** About 70 signed commands. Verified live on one EU Model Y (firmware 2026.38.6): `flash` (with wake), `charge-limit` (vehicle refusal `already_set`), `lock`, `unlock`, `climate-on` and `climate-off`. All other commands, other regions and other vehicles are covered by unit and end-to-end tests only. See [API deviations](../api-deviations.md) and the [protocol notes](../m3-command-protocol.md).

## Prerequisites

Commands only work when all of these are in place:

1. **Partner registration** for your domain and region — see [Partner Onboarding](partner-onboarding.md).
2. **Public key hosted** at `https://<domain>/.well-known/appspecific/com.tesla.3p.public-key.pem`. The matching private key is the one your app signs with (`TeslaKeyPair`).
3. **Virtual key installed on the vehicle.** The driver opens `https://tesla.com/_ak/<your-domain>` on a phone with the Tesla app and approves the key (confirmed in the car with an existing key card or phone key). Until then the vehicle rejects every command with `TeslaKeyNotPairedException`.
4. **OAuth scopes** `vehicle_cmds` (lock, closures, climate, modes) and `vehicle_charging_cmds` (charging and schedules) granted by the user at login (`TeslaScopes.VehicleCmds`, `TeslaScopes.VehicleChargingCmds`).
5. A vehicle that supports the Vehicle Command Protocol (see the error table below).

## Quickstart (standalone)

```csharp
using CoilPro;
using CoilPro.Commands;

using var keyPair = TeslaKeyPair.FromPrivateKeyPem(File.ReadAllText("private-key.pem"));

using var apiHttp = new HttpClient { BaseAddress = TeslaEndpoints.For(TeslaRegion.Europe).FleetApiBaseUrl };
ICoilProCommands commands = new CoilProCommands(apiHttp, tokenProvider, keyPair);

CommandResult result = await commands.FlashLightsAsync("5YJ3E1EA7KF000000"); // 17-character VIN
Console.WriteLine(result.Success ? "OK" : $"Refused: {result.Reason}");
```

Every method takes the **17-character VIN**, not the numeric Fleet API id. The full list is in the [command reference](#command-reference) below.

## Command reference

Every command is billed as a Fleet API *command* (plus a *wake* if `AutoWake` fires). "Security" commands use the vehicle's security controller (VCSEC domain); all others go to the infotainment computer. Parameter types (`SeatPosition`, `ScheduleDays`, ...) are in `CommandParameters.cs`.

### Security, access and closures

| C# method | CLI | Notes |
|---|---|---|
| `LockAsync` / `UnlockAsync` | `lock` / `unlock` | Security. Verified live. |
| `RemoteStartDriveAsync` | `remote-start` | Security. Enables keyless driving. |
| `HonkHornAsync` / `FlashLightsAsync` | `honk` / `flash` | `flash` verified live. |
| `PingAsync` | `ping` | Signed no-op: checks key pairing and that the infotainment answers. |
| `OpenFrunkAsync` | `frunk-open` | Security. There is no remote frunk close. |
| `ActuateRearTrunkAsync` | `trunk` | Security. A toggle: opens or closes. |
| `CloseRearTrunkAsync` | `trunk-close` | Security. Powered liftgate only. |
| `AutoSecureModelXAsync` | `auto-secure` | Model X: closes falcon-wing doors and locks. |
| `OpenTonneauAsync` / `CloseTonneauAsync` / `StopTonneauAsync` | `tonneau-open` / `tonneau-close` / `tonneau-stop` | Cybertruck only. |
| `VentWindowsAsync` / `CloseWindowsAsync` | `windows-vent` / `windows-close` | Vehicle must be parked. |

### Climate

| C# method | CLI | Notes |
|---|---|---|
| `StartClimateAsync` / `StopClimateAsync` | `climate-on` / `climate-off` | Verified live. |
| `SetTemperaturesAsync` | `temps <driverC> <passengerC>` | |
| `SetPreconditioningMaxAsync` | `defrost-max on\|off` | Maximum defrost. |
| `SetBioweaponModeAsync` | `bioweapon on\|off` | Bioweapon Defense Mode; Model S/X. |
| `SetCabinOverheatProtectionAsync` | `cop on\|off [--fan-only]` | |
| `SetCabinOverheatProtectionTemperatureAsync` | `cop-temp low\|medium\|high` | About 30 / 35 / 40 degrees C; does not turn the feature on. |
| `SetClimateKeeperModeAsync` | `climate-keeper off\|keep\|dog\|camp` | |
| `SetSeatHeaterAsync` | `seat-heater <seat> <0-3>` | Needs climate on. Seats: `fl fr rl rlb rc rr rrb 3l 3r`; rear and third-row seats depend on the model. |
| `SetSeatCoolerAsync` | `seat-cooler <left\|right> <0-3>` | Front seats of vehicles with ventilated seats. |
| `SetAutoSeatClimateAsync` | `auto-seat <left\|right> on\|off` | |
| `SetSteeringWheelHeaterAsync` | `wheel-heater on\|off` | On/off only; heat levels are not in Tesla's published protocol definitions. |

### Charging

| C# method | CLI | Notes |
|---|---|---|
| `StartChargingAsync` / `StopChargingAsync` | `charge-start` / `charge-stop` | |
| `SetChargeLimitAsync` | `charge-limit <percent>` | 50-100. Refusal `already_set` verified live. |
| `SetChargingAmpsAsync` | `charging-amps <amps>` | |
| `OpenChargePortAsync` / `CloseChargePortAsync` | `charge-port-open` / `charge-port-close` | |
| `ChargeStandardRangeAsync` / `ChargeMaxRangeAsync` | `charge-standard` / `charge-max` | |
| `AddChargeScheduleAsync` | `charge-schedule-add <days> <HH:MM-HH:MM> <lat> <lon> [--id n] [--once]` | Firmware 2024.26+. Bound to a location; `<days>` is `mon,tue,...` or `weekdays`, `weekend`, `all`. The id defaults to the current Unix time; reuse it to replace a schedule. |
| `RemoveChargeScheduleAsync` / `RemoveChargeSchedulesAsync` | `charge-schedule-remove <id>\|home\|work\|other` | One schedule by id, or all at the given kinds of locations. |
| `AddPreconditionScheduleAsync` | `precondition-schedule-add <days> <HH:MM> <lat> <lon> [--id n] [--once]` | Firmware 2024.26+. |
| `RemovePreconditionScheduleAsync` / `RemovePreconditionSchedulesAsync` | `precondition-schedule-remove <id>\|home\|work\|other` | |
| `SetScheduledChargingAsync` | `scheduled-charging on\|off <HH:MM>` | Legacy; Tesla recommends the schedules above from firmware 2024.26. |
| `SetScheduledDepartureAsync` / `ClearScheduledDepartureAsync` | `scheduled-departure <HH:MM> [precond] [offpeak] [end]` / `scheduled-departure-clear` | Legacy, see above. Policies: `off`, `all`, `weekdays`. |

### Power, media and modes

| C# method | CLI | Notes |
|---|---|---|
| `SetLowPowerModeAsync` | `low-power on\|off` | |
| `SetKeepAccessoryPowerModeAsync` | `accessory-power on\|off` | |
| `SetVolumeAsync` | `volume <0-10>` | |
| `VolumeUpAsync` / `VolumeDownAsync` | `volume-up` / `volume-down` | |
| `NextTrackAsync` / `PreviousTrackAsync` | `next-track` / `prev-track` | |
| `NextFavoriteAsync` / `PreviousFavoriteAsync` | `next-fav` / `prev-fav` | |
| `TogglePlaybackAsync` | `play-pause` | |
| `SetSentryModeAsync` | `sentry on\|off` | |
| `SetGuestModeAsync` / `EraseUserDataAsync` | `guest on\|off` / `erase-guest-data` | Erasing needs the car parked in guest mode. |
| `TriggerHomelinkAsync` | `homelink <lat> <lon>` | |
| `SetVehicleNameAsync` | `rename <name>` | |
| `ScheduleSoftwareUpdateAsync` / `CancelSoftwareUpdateAsync` | `update-schedule <minutes>` / `update-cancel` | An update must be available. |

### PINs, valet, speed limit and parental controls

| C# method | CLI | Notes |
|---|---|---|
| `EnableValetModeAsync` / `DisableValetModeAsync` / `ResetValetPinAsync` | `valet-on <pin>` / `valet-off` / `valet-reset-pin` | PINs have 4 digits. Resetting needs valet mode off. |
| `SetPinToDriveAsync` | `pin-to-drive on <pin>` / `pin-to-drive off` | |
| `ResetPinToDriveAsync` | `pin-to-drive-reset` | |
| `ClearPinToDriveAdminAsync` | `pin-to-drive-clear-admin` | Owner or fleet manager; firmware 2023.44+. |
| `SetSpeedLimitAsync` | `speed-limit <mph>` | mph, as in Tesla's API. |
| `ActivateSpeedLimitAsync` / `DeactivateSpeedLimitAsync` | `speed-limit-activate <pin>` / `speed-limit-deactivate <pin>` | |
| `ClearSpeedLimitPinAsync` | `speed-limit-clear-pin <pin>` | |
| `ClearSpeedLimitPinAdminAsync` | `speed-limit-clear-admin` | Owner or fleet manager; firmware 2023.38+. |
| `ActivateParentalControlsAsync` / `DeactivateParentalControlsAsync` | `parental-activate <pin>` / `parental-deactivate <pin>` | |
| `SetParentalControlsSettingAsync` | `parental-setting <name> on\|off` | While controls are inactive. Names: `speed-limit acceleration safety-features curfew browser-blocked theater-blocked arcade-blocked`. |
| `SetParentalControlsSpeedLimitAsync` | `parental-speed-limit <mph>` | |
| `ClearParentalControlsPinAdminAsync` | `parental-clear-admin` | Owner or fleet manager. |

### Navigation (not signed)

These are REST calls on `ICoilProClient`, not signed commands: Tesla processes them server-side. They are billed as commands and need no virtual key; see [Vehicle Endpoints](vehicle-endpoints.md).

| C# method | CLI | Notes |
|---|---|---|
| `ShareNavigationDestinationAsync` | `nav <destination text>` | |
| `NavigateToCoordinatesAsync` | `nav-gps <lat> <lon>` | Unverified on protocol-only vehicles. |
| `NavigateToSuperchargerAsync` | `nav-supercharger <id>` | Unverified on protocol-only vehicles. |

### Fleet API only

Everything here is sent through the Fleet API's `signed_command` endpoint. CoilPro does not speak Bluetooth, so features that exist only over BLE are out of reach (see below). Fleet Telemetry configuration is signed too, but is not a vehicle command; see [Fleet Telemetry](fleet-telemetry.md).

### Intentionally not supported

| Feature | Why |
|---|---|
| Adding a key to the vehicle (`add-key-request`) | Possible only over Bluetooth, not over the Fleet API. |
| Boombox / external speaker | Not in Tesla's published protocol definitions. |
| Steering wheel heater levels | Not in the published definitions; only on/off is available. |
| Calendar sync | Not in the published definitions. |
| Pre-2021 Model S/X and other vehicles without the Vehicle Command Protocol | They use the legacy REST commands, which CoilPro does not implement. |

## Dependency injection

`AddCoilProCommands` in `CoilPro.AspNetCore` registers `ICoilProCommands` (and `IFleetTelemetryConfigurator`). Call `AddCoilProApi` first — the commands reuse its Fleet API `HttpClient`, `ITeslaTokenProvider` and `ICoilProClient`.

```csharp
builder.Services.AddCoilProApi(o => { /* ClientId, Region, Scopes incl. VehicleCmds ... */ });

// Option A: register the key yourself ...
builder.Services.AddSingleton(TeslaKeyPair.FromPrivateKeyPem(builder.Configuration["Tesla:PrivateKeyPem"]!));
builder.Services.AddCoilProCommands(o => o.AutoWake = false);

// Option B: ... or pass the PEM directly
builder.Services.AddCoilProCommands(builder.Configuration["Tesla:PrivateKeyPem"]!, o => o.CommandTimeout = TimeSpan.FromSeconds(45));

// Option C: ... or a factory (e.g. secret store)
builder.Services.AddCoilProCommands(sp => LoadKeyFromVault(sp));
```

```csharp
public sealed class GarageService(ICoilProCommands commands)
{
    public Task<CommandResult> SetLimitAsync(string vin, int percent, CancellationToken ct) =>
        commands.SetChargeLimitAsync(vin, percent, ct);
}
```

Treat the private key like a password: it can unlock the car. Keep it in a secret store, never in source control.

## Results vs. exceptions

- A command the vehicle **received and authenticated but refused** returns `CommandResult { Success = false, Reason = ... }`. This is a normal outcome — for example `already_set`, `not_charging`, `is_charging`, `complete`, `disconnected`, `car_wash`, or `VEHICLE_NOT_IN_PARK` for security commands.
- A command that **could not be delivered or authenticated** throws `TeslaCommandException`, with `Vin`, `Fault` (`TeslaMessageFault`) and `StatusCode` where available. Two subtypes cover the cases you usually handle explicitly: `TeslaKeyNotPairedException` and `TeslaVehicleAsleepException`.

```csharp
try
{
    var result = await commands.LockAsync(vin);
    if (!result.Success) logger.LogInformation("Refused: {Reason}", result.Reason);
}
catch (TeslaKeyNotPairedException)  { /* ask the driver to open https://tesla.com/_ak/<domain> */ }
catch (TeslaVehicleAsleepException) { /* wake the car, or enable AutoWake */ }
catch (TeslaCommandException ex)    { logger.LogError(ex, "fault {Fault}, HTTP {Status}", ex.Fault, ex.StatusCode); }
```

Commands to the same VIN are serialized. Retryable protocol faults (busy, timeout, expired message, ...) are retried inside `CommandTimeout`.

## AutoWake is opt-in

By default a command to a sleeping vehicle throws `TeslaVehicleAsleepException` immediately. With `CoilProCommandsOptions.AutoWake = true`, CoilPro first calls `wake_up` and polls until the vehicle is online (bounded by `WakeTimeout`, default 60 s). It is opt-in because the Fleet API is pay-per-use and a wake-up is a billed call; it also wakes the car's computer, which costs a little battery. Nothing should spend money or energy without your decision. AutoWake needs an `ICoilProClient` (injected automatically with `AddCoilProCommands`; passed as constructor argument otherwise).

| Option | Default | Meaning |
|---|---|---|
| `AutoWake` | `false` | Wake a sleeping vehicle before sending. |
| `WakeTimeout` | 60 s | Max wait for the vehicle to come online. |
| `WakePollInterval` | 3 s | Interval between online checks. |
| `CommandTimeout` | 30 s | Overall deadline per command incl. handshake and retries. |
| `MessageLifetime` | 5 s | Expiry written into each signed message. |
| `RetryDelay` | 1 s | Delay before resending after a retryable fault. |

## Errors

| Situation | What you see | What to do |
|---|---|---|
| Virtual key not installed | `TeslaKeyNotPairedException` (fault `UNKNOWN_KEY_ID` or session status `KEY_NOT_ON_WHITELIST`) | Open `https://tesla.com/_ak/<your-domain>` in the Tesla app and approve the key in the car. |
| Vehicle asleep / offline | `TeslaVehicleAsleepException` (HTTP 503, or 408 "vehicle is offline") | Call `ICoilProClient.WakeUpAsync`, run `coilpro wake <VIN>`, or enable `AutoWake`. |
| Vehicle does not support the protocol | `TeslaCommandException` with `StatusCode = 422` | Pre-2021 Model S/X and similar vehicles use the legacy REST commands, which CoilPro does not implement. |
| Protocol fault from the vehicle | `TeslaCommandException` with `Fault` set | See `TeslaMessageFault`; retryable faults are already retried. |
| Missing scope / token | `TeslaCommandException` with `StatusCode` 401 or 403 | Log in again with `vehicle_cmds` and `vehicle_charging_cmds`. Tesla answers a missing scope with `403 Unauthorized missing scopes` (observed on other Fleet API endpoints; not yet observed on `signed_command`). |
| Vehicle refuses | `CommandResult.Success == false` | Read `Reason`; this is not an error. |

## CLI

The `coilpro` tool exposes the same commands:

```bash
coilpro keys generate                       # once; also needed for partner registration
coilpro login                               # default scopes include vehicle_cmds / vehicle_charging_cmds

coilpro cmd <VIN> flash                     # harmless first test
coilpro cmd <VIN> charge-limit 80
coilpro cmd <VIN> temps 21 21
coilpro cmd <VIN> climate-on --wake         # wake first if asleep (billed)
coilpro cmd <VIN> lock --timeout 60
```

Every command in the reference above has a CLI name (kebab-case). `coilpro cmd --help` lists them all with their arguments; `--id`, `--once` and `--fan-only` are the only extra options. Navigation commands (`nav`, `nav-gps`, `nav-supercharger`) need no private key.

The CLI loads the private key from `~/.coilpro/keys/private-key.pem` and the tokens/region from the last `coilpro login`. Output and exit codes:

| Output | Exit code |
|---|---|
| `OK` | 0 |
| other error, usage error | 1 |
| `Refused: <reason>` | 2 |
| key not paired (hint to open `https://tesla.com/_ak/<your registered domain>`) | 3 |
| vehicle asleep (hint: `coilpro wake <VIN>` or `--wake`) | 4 |
