# Changelog

All notable changes to CoilPro are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [Semantic Versioning](https://semver.org/)
(derived from git tags via MinVer).

## [Unreleased]

## [0.2.0] - date TBD

Towards the complete Fleet API: all vehicle, user, charging, energy, partner and Fleet Telemetry endpoints, about 70
signed commands, and CLI and DI support for all of it. First live run on the EU region with one Model Y covered the
vehicle and user REST endpoints; **energy endpoints, business-account endpoints and most new commands are covered by
tests only** (see [`docs/api-deviations.md`](./docs/api-deviations.md)).

### Added

- **Vehicle endpoints (`ICoilProClient`)**: `GetVehiclesPageAsync`, `GetVehicleAsync`, `GetVehicleDataCachedAsync`, `GetNearbyChargingSitesAsync`, `GetRecentAlertsAsync`, `GetReleaseNotesAsync`, service data and history, drivers, mobile access, share invitations, `GetFleetStatusAsync`, `GetApiStatusAsync`; `ResultPage<T>` for paged results.
- **Unsigned navigation commands**: `ShareNavigationDestinationAsync`, `NavigateToCoordinatesAsync`, `NavigateToSuperchargerAsync`, `SetNavigationWaypointsAsync`.
- **User, charging and management endpoints**: `GetMeAsync`, `GetOrdersAsync`, `GetRegionAsync`, charging invoices and fleet charging sessions, vehicle options, warranty, subscription and upgrade eligibility, enterprise roles and payer.
- **Fleet Telemetry**: read, delete and error endpoints on `ICoilProClient`; native signing in `IFleetTelemetryConfigurator` / `FleetTelemetryConfigurator` (Schnorr/P-256 JWS as Tesla's proxy does) to create or update configurations.
- **Energy (`ICoilProEnergyClient`)**: products, live status, site info and status, backup time, Wall Connector live status, tariff, backup / energy / charge history, and settings (backup reserve, operation mode, storm mode, grid import/export, time-of-use).
- **Partner endpoints (`ITeslaPartnerClient`)**: vehicle specs, model pricing, partner-wide telemetry errors and error VINs.
- **Scopes**: `VehicleSpecs`, `VehiclePricingInfo`, `EnterpriseManagement`.
- **Signed commands (`ICoilProCommands`)**: about 60 more — trunks, frunk and tonneau, windows, remote start, charge port close, charge schedules and precondition schedules, climate extras (max defrost, Bioweapon Defense, cabin overheat protection, climate keeper, seat heaters and coolers, steering wheel heater), media, Sentry, valet, PIN to Drive, speed limit, parental controls, guest mode, software update scheduling, HomeLink, rename, ping. Parameter types in `CommandParameters.cs`.
- **ASP.NET Core**: `AddCoilProApi` registers `ICoilProEnergyClient`; `AddCoilProCommands` registers `IFleetTelemetryConfigurator`.
- **CLI**: `coilpro cmd` supports all signed commands plus `nav`, `nav-gps`, `nav-supercharger` (kebab-case names, `--id`, `--once`, `--fan-only`); new read commands `status`, `cached`, `alerts`, `release-notes`, `service`, `drivers`, `nearby`, `charging-history`, `invoice`, `me`, `region`, `warranty`, `options`, `energy products|live`, `telemetry get|errors|delete`; help marks billed calls. `coilpro login` requests the energy scopes by default and `--scopes` accepts `vehicle_specs`, `vehicle_pricing_info` and `enterprise_management`.
- **Docs**: articles on vehicle endpoints, energy, Fleet Telemetry, and a full command reference.

### Changed

- `GetChargingHistoryAsync` now takes an optional `ChargingHistoryQuery` (VIN, time range, paging, sort) and returns `ChargingHistoryPage` with `Data`, `TotalResults` and `HasMoreData`.
- `Vehicle` and `VehicleData` (and their sub-states) model many more fields; unknown fields are still preserved in `AdditionalData`.
- `VehicleAlert.Time` is the raw string as Tesla sends it (RFC 1123, not ISO 8601); the parsed value is available as `VehicleAlert.TimeUtc`. The list field is `Audiences`.
- Fixed in the docs and READMEs: DI methods are `AddCoilProApi` and `AddCoilProCommands`.

### Known limitations

- Energy endpoints are untested against real energy hardware; business-account endpoints (`enterprise`, fleet charging sessions) are untested live.
- Navigation GPS, Supercharger and waypoint commands are unverified on protocol-only vehicles.
- Not supported: BLE-only key enrollment, boombox, steering wheel heat levels and calendar sync (not in Tesla's published protocol definitions).
- The Fleet Telemetry server is not part of CoilPro.

## [0.1.0] - 2026-11-24

First public release. Pre-1.0: the API may still change in minor versions. Verified live against the **EU region with one Model Y** (firmware 2026.38.6); other
regions and vehicles are covered by tests only. Deviations between Tesla's documentation and the real API are
recorded in [`docs/api-deviations.md`](./docs/api-deviations.md).

### Added

- **Core (`CoilPro`)**: regional Fleet API endpoints (EU, North America); `ITeslaAuthClient` with authorization code + PKCE, client credentials and refresh; `ITokenStore` / `InMemoryTokenStore` and `ITeslaTokenProvider` with automatic refresh.
- **Partner onboarding**: `TeslaKeyPair` (P-256 key generation, PEM export byte-identical to `openssl`, hex export), `ITeslaPartnerClient` (`RegisterAsync`, `GetPublicKeyAsync`, partner-token caching).
- **Read endpoints**: `ICoilProClient` with `GetVehiclesAsync`, `GetVehicleDataAsync` (per-segment `VehicleDataEndpoint` filter), `WakeUpAsync`, `GetChargingHistoryAsync`, `LastRateLimit`; tolerant record models; retry/backoff with `Retry-After` support (`CoilProClientOptions`); `TeslaApiException` and `RateLimitInfo`.
- **Vehicle Command Protocol (`CoilPro.Commands`)**: native C# implementation (ECDH session handshake, ECDSA, HMAC and AES-GCM, Protobuf envelope, response decryption, retry on protocol faults) behind `ICoilProCommands`: lock, unlock, honk, flash, start/stop climate, set temperatures, start/stop charging, set charge limit, open charge port, set charging amps. Opt-in `AutoWake`. Failures surface as `TeslaCommandException` (`TeslaKeyNotPairedException`, `TeslaVehicleAsleepException`); vehicle refusals as `CommandResult`.
- **ASP.NET Core (`CoilPro.AspNetCore`)**: `AddCoilProApi`, `AddCoilProCommands`, OAuth callback parsing helper.
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

[Unreleased]: https://github.com/machine1986/CoilPro/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/machine1986/CoilPro/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/machine1986/CoilPro/releases/tag/v0.1.0
