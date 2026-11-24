# Coil.NET

**Coil.NET** is an unofficial, open-source .NET SDK for the [Tesla Fleet API](https://developer.tesla.com/docs/fleet-api). It targets `net8.0` and `net10.0` and is MIT-licensed.

> **Unofficial — not affiliated with, endorsed by, or sponsored by Tesla, Inc.** "Tesla" is used only to describe API compatibility.

> **Preview.** `0.1.0-preview` — verified live against the EU region with one Model Y. Expect API changes before 1.0.

## What it does

- **Signed vehicle commands** via the native Vehicle Command Protocol (ECDSA P-256, ECDH, HMAC-SHA256 / AES-GCM, Protobuf) in pure C# — no Go-based `tesla-http-proxy` needed.
- **OAuth (authorization code + PKCE, client credentials, refresh)** and a guided **partner onboarding** path: key generation, public-key hosting, registration.
- **Read endpoints** (`ICoilClient`): vehicle list, `vehicle_data` with cost-conscious segment filters, wake-up, charging history, rate-limit tracking.
- **ASP.NET Core / DI** integration and a `coil` command-line tool for onboarding and smoke tests.

## Where to start

- [Getting Started](articles/getting-started.md) — install, build the authorization URL, exchange the code.
- [Partner Onboarding](articles/partner-onboarding.md) — the one-time setup Tesla requires before any call works.
- [Reading Vehicle Data](articles/reading-vehicle-data.md) and [Signed Vehicle Commands](articles/commands.md).
- [API Reference](api/index.md) — generated from the XML documentation.
- [API Deviations](api-deviations.md) — where Tesla's real API differed from its documentation or from our plan, with live-verification notes.

## Packages

| Package | Purpose |
|---|---|
| `Coil` | Core: OAuth, HTTP layer, partner onboarding, vehicle read endpoints, models |
| `Coil.Commands` | Vehicle Command Protocol (signed commands) |
| `Coil.AspNetCore` | `IServiceCollection` extensions (`AddCoilApi`, `AddCoilCommands`), OAuth callback helper |
| `Coil.Cli` | `dotnet tool` providing the `coil` command |

Source and issues: [github.com/machine1986/Coil.NET](https://github.com/machine1986/Coil.NET).
