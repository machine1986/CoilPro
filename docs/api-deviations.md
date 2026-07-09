# API Deviations

Coil.NET's technical specification (`doc/11 SDK – Technische Spezifikation.md`) describes endpoints, scopes, and protocol details as they were understood at planning time. Tesla changes the Fleet API's hosts, scopes, and payloads without much notice, so every endpoint, scope, and protocol detail is re-verified against [developer.tesla.com](https://developer.tesla.com) (and, for the Vehicle Command Protocol, [`teslamotors/vehicle-command`](https://github.com/teslamotors/vehicle-command)) before it ships in code.

This document is the record of what changed: whenever verification turns up a difference between what the spec assumed and what the real API actually requires, it gets logged here — with a link back to the source that confirmed it. Treat it as the changelog between "what we planned" and "what we shipped," and check it before trusting a hostname, scope name, or field from the spec doc at face value.

## Deviations

| Date | Area | Spec said | Actual | Source |
|---|---|---|---|---|
| 2026-07-09 | `vehicle_data` `endpoints` param | Not specified in the planning-time spec | Coil.NET's `VehicleDataEndpoint.ToQueryValue()` joins selected segments with `;` (e.g. `charge_state;drive_state`) as the `endpoints` query parameter | Status: to verify — [developer.tesla.com](https://developer.tesla.com) `vehicle_data` reference |
| 2026-07-09 | Charging-history endpoint path | Not specified in the planning-time spec | Assumed `api/1/dx/charging/history`; response modeled as `ChargingHistoryPage` (`data`, `totalResults`, `hasMoreData`) with per-record fields `vin`, `chargeStartDateTime`, `chargeStopDateTime`, `siteLocationName` | Status: to verify — path and shape are provisional, not confirmed against a live account |
| 2026-07-09 | Rate-limit header names | Not specified in the planning-time spec | `RateLimitInfo.FromHeaders` reads `x-ratelimit-limit` / `x-ratelimit-remaining` / `x-ratelimit-reset` (plus the standard `Retry-After`) | Status: to verify — Fleet API rate-limit header contract is not fully public |
| 2026-07-09 | Fleet API error body shape | Not specified in the planning-time spec | `TeslaApiException` parsing assumes `{ "error": "...", "error_description": "..." }` | Status: to verify — against actual error responses from `developer.tesla.com`-documented endpoints |
