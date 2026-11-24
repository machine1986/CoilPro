# CoilPro

Unofficial, open-source .NET SDK for the **Tesla Fleet API**: OAuth with PKCE, guided partner onboarding,
typed read endpoints, and **signed vehicle commands** through a native implementation of the Vehicle
Command Protocol in pure C# — without Tesla's Go-based `tesla-http-proxy`.

> **Free and open source (MIT).** The "Pro" in the name is the naming scheme of its author's products — there is no paid edition.

> **Unofficial — not affiliated with, endorsed by, or sponsored by Tesla, Inc.**
> "Tesla" is a trademark of Tesla, Inc. and is used here only to describe API compatibility.

> **Preview.** Verified live against the EU region with one Model Y; other regions and vehicles are
> covered by tests only. Expect API changes before 1.0.

Target frameworks: **.NET 8** and **.NET 10**. License: **MIT**.

## Packages

| Package | Contents |
|---|---|
| `CoilPro` | Core: OAuth, HTTP plumbing, partner onboarding, vehicle read endpoints, models |
| `CoilPro.Commands` | Vehicle Command Protocol — signed commands |
| `CoilPro.AspNetCore` | DI extensions (`AddCoilProProApi`, `AddCoilProProCommands`), OAuth callback helper |
| `CoilPro.Cli` | `dotnet tool` (`coilpro`): onboarding, key generation, smoke tests |

## Onboarding (once, before your first API call)

Tesla requires a developer application, a domain serving your P-256 public key, and a partner registration
(per region). Without them even read-only calls fail.

```bash
dotnet tool install --global CoilPro.Cli

coilpro keys generate
# host the public key at https://<domain>/.well-known/appspecific/com.tesla.3p.public-key.pem
coilpro partner register   --domain <domain> --client-id <id> --client-secret <secret> --region eu
coilpro partner public-key --domain <domain> --client-id <id> --client-secret <secret> --region eu
coilpro login --client-id <id> --client-secret <secret> --region eu --port 8080   # redirect URI: http://localhost:8080/callback
```

## Reading vehicle data

The Fleet API is pay-per-use: request only the segments you need.

```csharp
using CoilPro;

ICoilProClient coil = new CoilProClient(apiHttp, tokenProvider, new CoilProClientOptions()); // or inject via AddCoilProProApi

var vehicles = await coil.GetVehiclesAsync();
var data = await coil.GetVehicleDataAsync(
    vehicles[0].Vin, VehicleDataEndpoint.ChargeState | VehicleDataEndpoint.DriveState);

Console.WriteLine($"Battery: {data.ChargeState?.BatteryLevel}%");
```

Errors surface as `TeslaApiException` (status code, Tesla error code, rate-limit info, raw body).

## Signed commands

Requires partner registration, a hosted public key, the virtual key installed on the vehicle
(`https://tesla.com/_ak/<your-domain>`), and the `vehicle_cmds` / `vehicle_charging_cmds` scopes.

```csharp
using CoilPro.Commands;

using var keyPair = TeslaKeyPair.FromPrivateKeyPem(File.ReadAllText("private-key.pem"));
ICoilProCommands commands = new CoilProCommands(apiHttp, tokenProvider, keyPair);

CommandResult result = await commands.SetChargeLimitAsync("<17-character VIN>", 80);
```

A vehicle refusal comes back as `CommandResult { Success = false, Reason }`; delivery or authentication
failures throw `TeslaCommandException` (including `TeslaKeyNotPairedException` and
`TeslaVehicleAsleepException`). Waking a sleeping vehicle is opt-in (`CoilProCommandsOptions.AutoWake`) because
it is a billed call. With DI: `services.AddCoilProProCommands(...)` from `CoilPro.AspNetCore`.

## Documentation

Guides, the partner-onboarding walkthrough and the log of deviations between Tesla's documentation and the
real API: **https://machine1986.github.io/CoilPro/** (source: https://github.com/machine1986/CoilPro)
