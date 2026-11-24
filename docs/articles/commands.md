# Signed Vehicle Commands

`ICoilCommands` (package `Coil.Commands`) sends commands such as lock, flash lights or set charge limit to the vehicle using Tesla's **Vehicle Command Protocol**, implemented natively in C#. Commands are signed with your P-256 key and sent through the Fleet API's `signed_command` endpoint — no `tesla-http-proxy` sidecar required.

> **Unofficial — not affiliated with Tesla, Inc.**

> **Preview.** Verified live on one EU Model Y (firmware 2026.38.6): `flash`, `charge-limit` (vehicle refusal `already_set`), `lock` and `unlock`. All other commands, other regions and other vehicles are covered by unit and end-to-end tests only. See [API deviations](../api-deviations.md) and the [protocol notes](../m3-command-protocol.md).

## Prerequisites

Commands only work when all of these are in place:

1. **Partner registration** for your domain and region — see [Partner Onboarding](partner-onboarding.md).
2. **Public key hosted** at `https://<domain>/.well-known/appspecific/com.tesla.3p.public-key.pem`. The matching private key is the one your app signs with (`TeslaKeyPair`).
3. **Virtual key installed on the vehicle.** The driver opens `https://tesla.com/_ak/<your-domain>` on a phone with the Tesla app and approves the key (confirmed in the car with an existing key card or phone key). Until then the vehicle rejects every command with `TeslaKeyNotPairedException`.
4. **OAuth scopes** `vehicle_cmds` (lock, honk, flash, climate) and `vehicle_charging_cmds` (charging) granted by the user at login (`TeslaScopes.VehicleCmds`, `TeslaScopes.VehicleChargingCmds`).
5. A vehicle that supports the Vehicle Command Protocol (see the error table below).

## Quickstart (standalone)

```csharp
using Coil;
using Coil.Commands;

using var keyPair = TeslaKeyPair.FromPrivateKeyPem(File.ReadAllText("private-key.pem"));

using var apiHttp = new HttpClient { BaseAddress = TeslaEndpoints.For(TeslaRegion.Europe).FleetApiBaseUrl };
ICoilCommands commands = new CoilCommands(apiHttp, tokenProvider, keyPair);

CommandResult result = await commands.FlashLightsAsync("5YJ3E1EA7KF000000"); // 17-character VIN
Console.WriteLine(result.Success ? "OK" : $"Refused: {result.Reason}");
```

Every method takes the **17-character VIN**, not the numeric Fleet API id. Available commands: `LockAsync`, `UnlockAsync`, `HonkHornAsync`, `FlashLightsAsync`, `StartClimateAsync`, `StopClimateAsync`, `SetTemperaturesAsync`, `StartChargingAsync`, `StopChargingAsync`, `SetChargeLimitAsync`, `OpenChargePortAsync`, `SetChargingAmpsAsync`.

## Dependency injection

`AddCoilCommands` in `Coil.AspNetCore` registers `ICoilCommands`. Call `AddCoilApi` first — the commands reuse its Fleet API `HttpClient`, `ITeslaTokenProvider` and `ICoilClient`.

```csharp
builder.Services.AddCoilApi(o => { /* ClientId, Region, Scopes incl. VehicleCmds ... */ });

// Option A: register the key yourself ...
builder.Services.AddSingleton(TeslaKeyPair.FromPrivateKeyPem(builder.Configuration["Tesla:PrivateKeyPem"]!));
builder.Services.AddCoilCommands(o => o.AutoWake = false);

// Option B: ... or pass the PEM directly
builder.Services.AddCoilCommands(builder.Configuration["Tesla:PrivateKeyPem"]!, o => o.CommandTimeout = TimeSpan.FromSeconds(45));

// Option C: ... or a factory (e.g. secret store)
builder.Services.AddCoilCommands(sp => LoadKeyFromVault(sp));
```

```csharp
public sealed class GarageService(ICoilCommands commands)
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

By default a command to a sleeping vehicle throws `TeslaVehicleAsleepException` immediately. With `CoilCommandsOptions.AutoWake = true`, Coil first calls `wake_up` and polls until the vehicle is online (bounded by `WakeTimeout`, default 60 s). It is opt-in because the Fleet API is pay-per-use and a wake-up is a billed call; it also wakes the car's computer, which costs a little battery. Nothing should spend money or energy without your decision. AutoWake needs an `ICoilClient` (injected automatically with `AddCoilCommands`; passed as constructor argument otherwise).

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
| Vehicle asleep / offline | `TeslaVehicleAsleepException` (HTTP 503, or 408 "vehicle is offline") | Call `ICoilClient.WakeUpAsync`, run `coil wake <VIN>`, or enable `AutoWake`. |
| Vehicle does not support the protocol | `TeslaCommandException` with `StatusCode = 422` | Pre-2021 Model S/X and similar vehicles use the legacy REST commands, which Coil does not implement. |
| Protocol fault from the vehicle | `TeslaCommandException` with `Fault` set | See `TeslaMessageFault`; retryable faults are already retried. |
| Missing scope / token | `TeslaCommandException` with `StatusCode` 401 or 403 | Log in again with `vehicle_cmds` and `vehicle_charging_cmds`. Tesla answers a missing scope with `403 Unauthorized missing scopes` (observed on other Fleet API endpoints; not yet observed on `signed_command`). |
| Vehicle refuses | `CommandResult.Success == false` | Read `Reason`; this is not an error. |

## CLI

The `coil` tool exposes the same commands:

```bash
coil keys generate                       # once; also needed for partner registration
coil login                               # default scopes include vehicle_cmds / vehicle_charging_cmds

coil cmd <VIN> flash                     # harmless first test
coil cmd <VIN> charge-limit 80
coil cmd <VIN> temps 21 21
coil cmd <VIN> climate-on --wake         # wake first if asleep (billed)
coil cmd <VIN> lock --timeout 60
```

Commands: `lock`, `unlock`, `honk`, `flash`, `climate-on`, `climate-off`, `temps <driverC> <passengerC>`, `charge-start`, `charge-stop`, `charge-limit <percent>`, `charge-port-open`, `charging-amps <amps>`.

The CLI loads the private key from `~/.coil/keys/private-key.pem` and the tokens/region from the last `coil login`. Output and exit codes:

| Output | Exit code |
|---|---|
| `OK` | 0 |
| other error, usage error | 1 |
| `Refused: <reason>` | 2 |
| key not paired (hint to open `https://tesla.com/_ak/<your registered domain>`) | 3 |
| vehicle asleep (hint: `coil wake <VIN>` or `--wake`) | 4 |
