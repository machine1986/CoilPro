# Changelog

All notable changes to CoilPro are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [Semantic Versioning](https://semver.org/)
(derived from git tags via MinVer).

## [Unreleased]

## [0.1.0-preview.1] - date TBD

First public preview. Verified live against the **EU region with one Model Y** (firmware 2026.38.6); other
regions and vehicles are covered by tests only. Deviations between Tesla's documentation and the real API are
recorded in [`docs/api-deviations.md`](./docs/api-deviations.md).

### Added

- **Core (`CoilPro`)**: regional Fleet API endpoints (EU, North America); `ITeslaAuthClient` with authorization code + PKCE, client credentials and refresh; `ITokenStore` / `InMemoryTokenStore` and `ITeslaTokenProvider` with automatic refresh.
- **Partner onboarding**: `TeslaKeyPair` (P-256 key generation, PEM export byte-identical to `openssl`, hex export), `ITeslaPartnerClient` (`RegisterAsync`, `GetPublicKeyAsync`, partner-token caching).
- **Read endpoints**: `ICoilProClient` with `GetVehiclesAsync`, `GetVehicleDataAsync` (per-segment `VehicleDataEndpoint` filter), `WakeUpAsync`, `GetChargingHistoryAsync`, `LastRateLimit`; tolerant record models; retry/backoff with `Retry-After` support (`CoilProClientOptions`); `TeslaApiException` and `RateLimitInfo`.
- **Vehicle Command Protocol (`CoilPro.Commands`)**: native C# implementation (ECDH session handshake, ECDSA, HMAC and AES-GCM, Protobuf envelope, response decryption, retry on protocol faults) behind `ICoilProCommands`: lock, unlock, honk, flash, start/stop climate, set temperatures, start/stop charging, set charge limit, open charge port, set charging amps. Opt-in `AutoWake`. Failures surface as `TeslaCommandException` (`TeslaKeyNotPairedException`, `TeslaVehicleAsleepException`); vehicle refusals as `CommandResult`.
- **ASP.NET Core (`CoilPro.AspNetCore`)**: `AddCoilProProApi`, `AddCoilProProCommands`, OAuth callback parsing helper.
- **CLI (`CoilPro.Cli`, command `coilpro`)**: `keys generate|show`, `partner register|public-key`, `login` (loopback redirect), `vehicles`, `data`, `wake`, `cmd`.
- DocFX documentation site, getting-started / onboarding / vehicle-data / commands articles.

### Verified live (EU, one Model Y)

- Partner registration (EU) and registered-key comparison; client-credentials and authorization-code login; `vehicles`, `vehicle_data`, charging history, `fleet_status`.
- Signed commands: `flash` (Infotainment domain), `charge-limit` (vehicle refusal `already_set` returned as `CommandResult`), `unlock` / `lock` (VCSEC domain).

### Fixed (found during live verification)

- `vehicle_data` `endpoints` must be `;`-separated; a comma-separated value silently returns no sub-states.
- Charging-history responses have no `response` envelope; previously always returned an empty page.
- Rate-limit headers are per bucket (`ratelimit-<bucket>-limit|remaining|reset`, reset in seconds); `LastRateLimit` was always null.
- Client-credentials `audience` must be the bare origin without a trailing slash; the code exchange now also sends `audience`.
- The interactive authorize page is `auth.tesla.com`; only the token endpoint lives on `fleet-auth`.

### Known limitations

- Only the EU region and a single Model Y have been exercised live; North America is unregistered and untested.
- Encrypted protocol fault replies have not been observed live.
- Pre-2021 Model S/X and other vehicles that do not support the Vehicle Command Protocol (legacy REST commands) are not supported.
- Public API surface may still change before 1.0.

[Unreleased]: https://github.com/machine1986/CoilPro/compare/v0.1.0-preview.1...HEAD
[0.1.0-preview.1]: https://github.com/machine1986/CoilPro/releases/tag/v0.1.0-preview.1
