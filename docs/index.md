# CoilPro

**CoilPro** is an unofficial, open-source .NET SDK for the [Tesla Fleet API](https://developer.tesla.com/docs/fleet-api). It targets `net10.0` and is MIT-licensed.

> **Unofficial — not affiliated with, endorsed by, or sponsored by Tesla, Inc.** "Tesla" is used only to describe API compatibility.

> **Status.** `0.2.0` — verified live against the EU region with one Model Y; energy endpoints not yet tested against real hardware. Expect API changes before 1.0.

## What it does

- **Signed vehicle commands** via the native Vehicle Command Protocol (ECDSA P-256, ECDH, HMAC-SHA256 / AES-GCM, Protobuf) in pure C# — no Go-based `tesla-http-proxy` needed.
- **OAuth (authorization code + PKCE, client credentials, refresh)** and a guided **partner onboarding** path: key generation, public-key hosting, registration.
- **The complete Fleet API surface**: vehicle, user, charging and vehicle-management endpoints (`ICoilProClient`), energy products (`ICoilProEnergyClient`), partner endpoints, Fleet Telemetry configuration with native signing, and about 70 signed commands. Cost-conscious by design: free and billed calls are marked, segment filters keep `vehicle_data` cheap.
- **ASP.NET Core / DI** integration and a `coilpro` command-line tool for onboarding and smoke tests.

## Where to start

- [Getting Started](articles/getting-started.md) — install, build the authorization URL, exchange the code.
- [Partner Onboarding](articles/partner-onboarding.md) — the one-time setup Tesla requires before any call works.
- [Reading Vehicle Data](articles/reading-vehicle-data.md), [Vehicle Endpoints](articles/vehicle-endpoints.md), [Energy Products](articles/energy.md), [Signed Vehicle Commands](articles/commands.md) and [Fleet Telemetry](articles/fleet-telemetry.md).
- [API Reference](api/index.md) — generated from the XML documentation.
- [API Deviations](api-deviations.md) — where Tesla's real API differed from its documentation or from our plan, with live-verification notes.

## Packages

| Package | Purpose |
|---|---|
| `CoilPro` | Core: OAuth, HTTP layer, partner onboarding, vehicle read endpoints, models |
| `CoilPro.Commands` | Vehicle Command Protocol (signed commands) |
| `CoilPro.AspNetCore` | `IServiceCollection` extensions (`AddCoilProApi`, `AddCoilProCommands`), OAuth callback helper |
| `CoilPro.Cli` | `dotnet tool` providing the `coilpro` command |

Source and issues: [github.com/machine1986/CoilPro](https://github.com/machine1986/CoilPro).
