# Coil.NET

Unofficial, open-source .NET SDK for the **Tesla Fleet API** — typed models, OAuth with PKCE, guided
partner onboarding, and (from v1.x) a native **Vehicle Command Protocol** implementation in pure C#,
without Tesla's Go-based `tesla-http-proxy`.

> ⚠️ **Unofficial — not affiliated with, endorsed by, or sponsored by Tesla, Inc.**
> "Tesla" is a trademark of Tesla, Inc. and is used here only to describe API compatibility.

Target frameworks: **.NET 8** and **.NET 10**. License: **MIT**.

## Packages

| Package | Contents |
|---|---|
| `Coil` | Core: auth, HTTP plumbing, vehicle read endpoints, models |
| `Coil.Commands` | Vehicle Command Protocol — signed commands (v1.x) |
| `Coil.AspNetCore` | DI extensions (`AddCoilApi`), OAuth callback helpers |
| `Coil.Cli` | `dotnet tool`: onboarding, key generation, smoke tests |

## Onboarding (once, before your first API call)

Tesla requires a one-time partner registration with a publicly hosted P-256 public key. Without it,
even read-only calls fail. The `coil` CLI walks you through it:

```bash
dotnet tool install --global Coil.Cli

coil keys generate                                    # P-256 key pair + ready-to-serve PEM
# host public-key.pem at:
#   https://<domain>/.well-known/appspecific/com.tesla.3p.public-key.pem
coil partner register   --domain <domain> --region eu
coil partner public-key --domain <domain> --region eu # verify what Tesla stored
```

Registration is **per region** — serving both EU and NA means running it twice.

## Quickstart

```bash
dotnet add package Coil
```

```csharp
using Coil;

var options = new TeslaAuthOptions
{
    ClientId = "REPLACE_WITH_YOUR_CLIENT_ID",
    RedirectUri = new Uri("http://localhost:8080/callback"),
    Region = TeslaRegion.Europe, // or TeslaRegion.NorthAmerica
    Scopes = TeslaScopes.Openid | TeslaScopes.OfflineAccess | TeslaScopes.VehicleDeviceData,
};

using var http = new HttpClient();
ITeslaAuthClient authClient = new TeslaAuthClient(http, options);

// 1. Send the user here to log in and grant access.
var request = authClient.CreateAuthorizationRequest();
Console.WriteLine($"Open this URL and log in: {request.AuthorizationUrl}");

// 2. After the redirect (?code=...), exchange it for tokens.
var tokens = await authClient.ExchangeCodeAsync(code, request.Pkce.Verifier);
```

## Reading vehicle data

The Fleet API is pay-per-use. Request only the segments you need via `VehicleDataEndpoint`, and don't
wake sleeping vehicles unnecessarily:

```csharp
ICoilClient coil = new CoilClient(http, tokenProvider); // or inject via AddCoilApi

var vehicles = await coil.GetVehiclesAsync();
var data = await coil.GetVehicleDataAsync(
    vehicles[0].Vin,
    VehicleDataEndpoint.ChargeState | VehicleDataEndpoint.DriveState);

Console.WriteLine($"Battery: {data.ChargeState?.BatteryLevel}%");
Console.WriteLine($"Rate limit: {coil.LastRateLimit?.Remaining}/{coil.LastRateLimit?.Limit}");
```

Errors surface as `TeslaApiException` (status code, Tesla error code, rate-limit info, raw body).

## Signed commands

`ICoilCommands` (package `Coil.Commands`) sends commands over the native Vehicle Command Protocol — no
`tesla-http-proxy`. You need partner registration, a hosted public key, the virtual key installed on the
vehicle (`https://tesla.com/_ak/<your-domain>`), and the `vehicle_cmds` / `vehicle_charging_cmds` scopes.

```csharp
using var keyPair = TeslaKeyPair.FromPrivateKeyPem(File.ReadAllText("private-key.pem"));
ICoilCommands commands = new CoilCommands(apiHttp, tokenProvider, keyPair);

CommandResult result = await commands.SetChargeLimitAsync("<VIN>", 80); // 17-character VIN
```

A vehicle refusal comes back as `CommandResult { Success = false, Reason }`; delivery or authentication
failures throw `TeslaCommandException` (including `TeslaKeyNotPairedException` and
`TeslaVehicleAsleepException`). Waking a sleeping vehicle is opt-in (`CoilCommandsOptions.AutoWake`) because
it is a billed call. With DI: `services.AddCoilCommands(...)` from `Coil.AspNetCore`. CLI:
`coil cmd <VIN> flash`.

## Documentation

Full docs, the partner-onboarding walkthrough, and the running log of deviations between Tesla's
published spec and the real API live in the repository:

**https://github.com/machine1986/Coil**
