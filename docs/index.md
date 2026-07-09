# Coil.NET

**Coil.NET** is an unofficial, open-source .NET SDK for the [Tesla Fleet API](https://developer.tesla.com/docs/fleet-api). It targets `net8.0` and `net10.0`, ships under the MIT license, and focuses on two things the .NET ecosystem has been missing:

- A **native Vehicle Command Protocol** implementation (signed commands: ECDSA P-256, ECDH, HMAC-SHA256, Protobuf envelope) in pure C# — no dependency on Tesla's Go-based `tesla-http-proxy`.
- A well-documented, low-friction **OAuth + partner onboarding** path — the step most third-party integrations get stuck on.

> **Unofficial — not affiliated with, endorsed by, or sponsored by Tesla, Inc.** "Tesla" is used only to describe API compatibility.

## Where to start

- New to Coil.NET? Read [Getting Started](articles/getting-started.md) — a five-minute quickstart that gets you from `dotnet add package Coil` to a printed authorization URL and an exchanged token.
- Building an integration you'll ship to real users? Read [Partner Onboarding](articles/partner-onboarding.md) for the full picture: keypair generation, `/.well-known` hosting, and Tesla partner registration.
- Looking for a specific type or member? See the [API Reference](api/index.md).

## Packages

| Package | Purpose |
|---|---|
| `Coil` | Core package: OAuth flows, HTTP layer, vehicle endpoints |
| `Coil.Commands` | Vehicle Command Protocol (signed commands) — the USP, ships in M3 |
| `Coil.AspNetCore` | `IServiceCollection` DI extensions, OAuth callback helpers |

The `coil` `dotnet tool` (source: `tools/Coil.Cli`) wraps common onboarding and smoke-test tasks — see [Partner Onboarding](articles/partner-onboarding.md) for the commands it will expose as they ship.

## Project status

Coil.NET is under active milestone-based development; see the [technical specification](https://github.com/machine1986/Coil/blob/main/doc/11%20SDK%20%E2%80%93%20Technische%20Spezifikation.md) and [`docs/api-deviations.md`](api-deviations.md) for where the implementation has diverged from Tesla's published API after verification.
