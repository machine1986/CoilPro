# CoilPro

[![CI](https://github.com/machine1986/CoilPro/actions/workflows/ci.yml/badge.svg)](https://github.com/machine1986/CoilPro/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/CoilPro.svg)](https://www.nuget.org/packages/CoilPro/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](./LICENSE)

**CoilPro** is an unofficial, open-source .NET SDK for the [Tesla Fleet API](https://developer.tesla.com/docs/fleet-api).
Its main differentiator is a **native implementation of the Vehicle Command Protocol in pure C#**: signed
vehicle commands without running Tesla's Go-based `tesla-http-proxy`.

> **Free and open source (MIT).** The "Pro" in the name is the naming scheme of its author's products — there is no paid edition.

> **Unofficial — not affiliated with, endorsed by, or sponsored by Tesla, Inc.**
> "Tesla" is a trademark of Tesla, Inc. and is used here only to describe API compatibility.

> **Preview (`0.1.0-preview`).** Verified live against the **EU region with one Model Y**. Other regions,
> vehicle models and most individual commands are covered by tests only. Expect API changes before 1.0.
> Details: [`docs/api-deviations.md`](./docs/api-deviations.md).

## Features

- **Signed commands** (`CoilPro.Commands`): lock/unlock, honk, flash, climate, charging control, charge limit, charge port, charging amps — ECDH session handshake, ECDSA P-256, HMAC and AES-GCM, Protobuf envelope, using `System.Security.Cryptography` only.
- **OAuth**: authorization code + PKCE, client credentials, refresh; token store and token provider abstractions.
- **Partner onboarding**: P-256 key generation, domain registration, registered-key verification (`coilpro` CLI and `ITeslaPartnerClient`).
- **Read endpoints**: vehicle list, `vehicle_data` with per-segment filters (the Fleet API is pay-per-use), wake-up, charging history, rate-limit tracking, tolerant model deserialization.
- **ASP.NET Core / DI** integration (`AddCoilProProApi`, `AddCoilProProCommands`) with `IHttpClientFactory` and Polly retries.
- **`coilpro` CLI** (`dotnet tool`) for onboarding and smoke tests.

Libraries target `net8.0` and `net10.0` (CLI: `net10.0`). MIT licensed.

## Packages

| Package | Contents |
|---|---|
| [`CoilPro`](https://www.nuget.org/packages/CoilPro/) | Core: OAuth, HTTP layer, partner onboarding, vehicle read endpoints, models |
| [`CoilPro.Commands`](https://www.nuget.org/packages/CoilPro.Commands/) | Vehicle Command Protocol — signed commands |
| [`CoilPro.AspNetCore`](https://www.nuget.org/packages/CoilPro.AspNetCore/) | DI extensions, OAuth callback helper |
| [`CoilPro.Cli`](https://www.nuget.org/packages/CoilPro.Cli/) | `dotnet tool` providing the `coilpro` command |

## Quickstart

### Prerequisites

Tesla requires a one-time setup before any Fleet API call works. The
[Partner Onboarding article](./docs/articles/partner-onboarding.md) covers every step and the error messages
you may meet.

1. A **Tesla developer application** at [developer.tesla.com](https://developer.tesla.com) (Client ID and secret). Register a redirect URI such as `http://localhost:8080/callback` — Tesla accepts `localhost`, not `127.0.0.1` — and your domain as an Allowed Origin.
2. A **domain** that serves your public key at `https://<domain>/.well-known/appspecific/com.tesla.3p.public-key.pem`.
3. **Partner registration** of that domain (per region — EU and North America are separate).
4. For commands: the **virtual key** installed on the vehicle by opening `https://tesla.com/_ak/<domain>` on a phone with the Tesla app.

### Onboard with the CLI

```bash
dotnet tool install --global CoilPro.Cli

coilpro keys generate                       # P-256 key pair in ~/.coilpro/keys/
# publish ~/.coilpro/keys/public-key.pem at the well-known URL above, then:
coilpro partner register   --domain <domain> --client-id <id> --client-secret <secret> --region eu
coilpro partner public-key --domain <domain> --client-id <id> --client-secret <secret> --region eu
coilpro login --client-id <id> --client-secret <secret> --region eu --port 8080
coilpro vehicles
coilpro cmd <VIN> flash --wake              # signed command (needs the virtual key)
```

### Read data from code

```bash
dotnet add package CoilPro
```

```csharp
using CoilPro;

var auth = new TeslaAuthOptions
{
    ClientId = "<client id>",
    ClientSecret = "<client secret>",
    RedirectUri = new Uri("http://localhost:8080/callback"),
    Region = TeslaRegion.Europe,
    Scopes = TeslaScopes.Openid | TeslaScopes.OfflineAccess | TeslaScopes.VehicleDeviceData,
};

using var authHttp = new HttpClient();
ITeslaAuthClient authClient = new TeslaAuthClient(authHttp, auth);

// 1. Send the user to the authorization URL; Tesla redirects back with ?code=...
var request = authClient.CreateAuthorizationRequest();
Console.WriteLine(request.AuthorizationUrl);

// 2. Exchange the code and keep the tokens in a store (use your own ITokenStore in real apps).
var tokens = await authClient.ExchangeCodeAsync(code, request.Pkce.Verifier);
ITokenStore store = new InMemoryTokenStore();
await store.SaveAsync(tokens);
ITeslaTokenProvider tokenProvider = new TeslaTokenProvider(store, authClient);

// 3. Read vehicle data — request only the segments you need.
using var apiHttp = new HttpClient { BaseAddress = TeslaEndpoints.For(TeslaRegion.Europe).FleetApiBaseUrl };
ICoilProClient coil = new CoilProClient(apiHttp, tokenProvider, new CoilProClientOptions());

var vehicles = await coil.GetVehiclesAsync();
var data = await coil.GetVehicleDataAsync(
    vehicles[0].Vin, VehicleDataEndpoint.ChargeState | VehicleDataEndpoint.DriveState);
Console.WriteLine($"Battery: {data.ChargeState?.BatteryLevel}%");
```

### Send a signed command

```csharp
using CoilPro.Commands;

// Request TeslaScopes.VehicleCmds | TeslaScopes.VehicleChargingCmds at login.
using var keyPair = TeslaKeyPair.FromPrivateKeyPem(File.ReadAllText("private-key.pem"));
ICoilProCommands commands = new CoilProCommands(apiHttp, tokenProvider, keyPair);

CommandResult result = await commands.SetChargeLimitAsync("<17-character VIN>", 80);
Console.WriteLine(result.Success ? "OK" : $"Refused: {result.Reason}");
```

A vehicle refusal is returned as `CommandResult { Success = false, Reason }`; delivery or authentication
failures throw `TeslaCommandException` (including `TeslaKeyNotPairedException` and
`TeslaVehicleAsleepException`). Waking a sleeping car is opt-in (`CoilProCommandsOptions.AutoWake`) because it
is a billed call. In ASP.NET Core use `services.AddCoilProProApi(...)` and `services.AddCoilProProCommands(...)`.

## Documentation

- [Getting Started](./docs/articles/getting-started.md)
- [Partner Onboarding](./docs/articles/partner-onboarding.md)
- [Reading Vehicle Data](./docs/articles/reading-vehicle-data.md)
- [Signed Vehicle Commands](./docs/articles/commands.md)
- [API Deviations](./docs/api-deviations.md) — where the real API differed from Tesla's documentation, with live-verification notes
- [Protocol notes](./docs/m3-command-protocol.md)
- API reference: built with DocFX (`dotnet tool restore && dotnet docfx docs/docfx.json`) and published to GitHub Pages once Pages is enabled.

## Samples

- [`samples/ConsoleQuickstart`](./samples/ConsoleQuickstart) — OAuth login from a console app.
- [`samples/AspNetCoreQuickstart`](./samples/AspNetCoreQuickstart) — ASP.NET Core integration with DI.

## Contributing, security, license

- [CONTRIBUTING.md](./CONTRIBUTING.md) · [SECURITY.md](./SECURITY.md) · [CHANGELOG.md](./CHANGELOG.md)
- Licensed under the [MIT License](./LICENSE). The Vehicle Command Protocol implementation is a C# port of Tesla's Apache-2.0 [`vehicle-command`](https://github.com/teslamotors/vehicle-command) project and includes its protobuf definitions — see [THIRD-PARTY-NOTICES.md](./THIRD-PARTY-NOTICES.md).

The private key you generate can unlock a vehicle. Keep it out of source control and treat it like a password.
