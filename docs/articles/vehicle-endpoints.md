# Vehicle, User and Account Endpoints

`ICoilProClient` covers the Fleet API REST endpoints that take a user token: vehicles, users, charging and vehicle management, plus the unsigned navigation commands. Energy products live in [`ICoilProEnergyClient`](energy.md); partner-token endpoints in `ITeslaPartnerClient`; signed commands in [`ICoilProCommands`](commands.md).

> **Unofficial — not affiliated with Tesla, Inc.**

> **Status (0.2.0).** Verified live on the EU region against one Model Y (mostly while the vehicle was offline). Endpoints that need an online vehicle, a business account or a partner token are covered by tests only; the notes below say which. See [API deviations](../api-deviations.md) for every place the real API differed from Tesla's documentation.

## Billing at a glance

The Fleet API is pay-per-use. The table marks each call as **free** or by the billing category it falls into:

| Category | Calls |
|---|---|
| **device data** | `GetVehicleDataAsync`, `GetNearbyChargingSitesAsync` |
| **wake** | `WakeUpAsync` |
| **commands** | all signed commands, the four navigation REST commands |
| **free** | everything else in the tables below |

Rules of thumb: prefer the free `GetVehicleDataCachedAsync` when stale data is acceptable, never wake a car just to read data, and request only the `vehicle_data` segments you need (see [Reading Vehicle Data](reading-vehicle-data.md)). Check Tesla's current price list on developer.tesla.com; CoilPro only reports the category.

All vehicle methods take the **17-character VIN**.

## Vehicles

| Method | Endpoint | Billing | Notes |
|---|---|---|---|
| `GetVehiclesAsync` | `GET vehicles` | free | First page only. |
| `GetVehiclesPageAsync` | `GET vehicles` | free | Returns `ResultPage<Vehicle>` with pagination. |
| `GetVehicleAsync` | `GET vehicles/{vin}` | free | Summary; does not wake the car. |
| `GetVehicleDataAsync` | `GET vehicles/{vin}/vehicle_data` | device data | Vehicle must be online. `endpoints` are joined with `;` — a comma silently returns no sub-states. |
| `GetVehicleDataCachedAsync` | `GET vehicles/{vin}/vehicle_data_cached` | free | Last reported data; works while the car sleeps. No location or drive state, no top-level vehicle fields; check each block's timestamp. |
| `WakeUpAsync` | `POST vehicles/{vin}/wake_up` | wake | Asynchronous on Tesla's side. |
| `GetFleetStatusAsync` | `POST vehicles/fleet_status` | free | Key paired?, firmware, whether the signed protocol is required. A cheap "is the virtual key installed?" check; one VIN per call is recommended. |
| `GetNearbyChargingSitesAsync` | `GET …/nearby_charging_sites` | device data | Online vehicle needed; `NearbyChargingSitesQuery` sets count and radius. |
| `GetRecentAlertsAsync` | `GET …/recent_alerts` | free | `VehicleAlert.Time` is an RFC 1123 string as sent by Tesla; `TimeUtc` parses it. List field is `audiences`. |
| `GetReleaseNotesAsync` | `GET …/release_notes` | free | `staged: true` for the upcoming firmware. |
| `GetServiceDataAsync` | `GET …/service_data` | free | Current service status. |
| `GetServiceHistoryAsync` | `GET …/service_history` | free | Paged, page size 1–50. |
| `GetServiceVisitAsync` | `GET …/service_history/{id}` | free | Work items and cost. |
| `GetDriversAsync` | `GET …/drivers` | free | Owner only. |
| `RemoveDriverAsync` | `DELETE …/drivers` | free | Without a share user id, removes the caller's own share. |
| `GetMobileEnabledAsync` | `GET …/mobile_enabled` | free | Answers 408 "vehicle unavailable" while the car is offline. |
| `GetShareInvitesAsync` | `GET …/invitations` | free | Paged, up to 25 per page. |
| `CreateShareInviteAsync` | `POST …/invitations` | free | Single-use, valid 24 hours. |
| `RedeemShareInviteAsync` | `POST invitations/redeem` | free | Redeems for the authenticated user. |
| `RevokeShareInviteAsync` | `POST …/invitations/{id}/revoke` | free | |
| `GetApiStatusAsync` | `GET /status` | free | Unauthenticated health check; returns `true` on `ok`. |

## Navigation (unsigned REST commands)

Tesla processes these server-side, so they need no virtual key and no signing. They are billed as commands.

| Method | Endpoint | Notes |
|---|---|---|
| `ShareNavigationDestinationAsync` | `command/navigation_request` | Address or place text. |
| `NavigateToCoordinatesAsync` | `command/navigation_gps_request` | Unverified on protocol-only vehicles. |
| `NavigateToSuperchargerAsync` | `command/navigation_sc_request` | Unverified on protocol-only vehicles. |
| `SetNavigationWaypointsAsync` | `command/navigation_waypoints_request` | Google place ids as `refId:<placeId>,…`. Unverified on protocol-only vehicles. |

All four return a `RestCommandResult` (`Result`, `Reason`). A `false` result is a refusal, not an exception.

## Fleet Telemetry configuration (read side)

| Method | Endpoint | Billing | Notes |
|---|---|---|---|
| `GetFleetTelemetryConfigAsync` | `GET …/fleet_telemetry_config` | free | `Config` is `null` when none is set. |
| `DeleteFleetTelemetryConfigAsync` | `DELETE …/fleet_telemetry_config` | free | Returns the number of updated vehicles. |
| `GetFleetTelemetryErrorsAsync` | `GET …/fleet_telemetry_errors` | free | |

Creating a configuration requires signing; see [Fleet Telemetry](fleet-telemetry.md).

## Users

| Method | Endpoint | Billing | Notes |
|---|---|---|---|
| `GetMeAsync` | `GET users/me` | free | Scope `user_data`. |
| `GetOrdersAsync` | `GET users/orders` | free | |
| `GetRegionAsync` | `GET users/region` | free | Region and base URL for this account. |

## Charging

| Method | Endpoint | Billing | Notes |
|---|---|---|---|
| `GetChargingHistoryAsync` | `GET dx/charging/history` | free | Optional `ChargingHistoryQuery` (VIN, time range, page number/size, sort). No `response` envelope. |
| `GetChargingInvoiceAsync` | `GET dx/charging/invoice/{contentId}` | free | Returns the PDF bytes; the id comes from `ChargingInvoice.ContentId`. |
| `GetChargingSessionsAsync` | `GET dx/charging/sessions` | free | Business accounts only; covered by tests only. |

## Vehicle management

| Method | Endpoint | Billing | Notes |
|---|---|---|---|
| `GetVehicleOptionsAsync` | `GET dx/vehicles/options` | free | Option codes. |
| `GetWarrantyDetailsAsync` | `GET dx/warranty/details` | free | Active, upcoming and expired coverage. |
| `GetSubscriptionEligibilityAsync` | `GET dx/vehicles/subscriptions/eligibility` | free | |
| `GetUpgradeEligibilityAsync` | `GET dx/vehicles/upgrades/eligibility` | free | Pricing includes a tax flag. |
| `GetEnterpriseRolesAsync` | `GET dx/enterprise/v1/{vin}/roles` | free | Business accounts; scope `enterprise_management`; unverified. |
| `SetEnterprisePayerAsync` | `POST dx/enterprise/v1/{vin}/payer` | free | Business accounts; unverified. |

All `dx/*` endpoints answer bare JSON (no `response` envelope); CoilPro accepts both shapes.

## Partner-token endpoints (`ITeslaPartnerClient`)

| Method | Endpoint | Billing | Notes |
|---|---|---|---|
| `GetVehicleSpecsAsync` | `GET vehicles/{vin}/specs` | per result | Any VIN, incl. battery state of health; scope `vehicle_specs` on the partner token. Check Tesla's price list. |
| `GetVehiclePricingAsync` | `POST dx/vehicles/pricing` | free | Model prices per market; scope `vehicle_pricing_info`. |
| `GetFleetTelemetryErrorsAsync` | `GET partner_accounts/fleet_telemetry_errors` | free | Across all vehicles of the application. |
| `GetFleetTelemetryErrorVinsAsync` | `GET partner_accounts/fleet_telemetry_error_vins` | free | |

## Scopes

`TeslaScopes` now also has `VehicleSpecs`, `VehiclePricingInfo` and `EnterpriseManagement`. A missing scope shows up as `403 Unauthorized missing scopes` (`TeslaApiException`). Request only scopes your application is configured for on developer.tesla.com.

## From the CLI

Most of the read calls have a `coilpro` command: `status`, `cached`, `alerts`, `release-notes`, `service`, `drivers`, `nearby`, `charging-history`, `invoice`, `me`, `region`, `warranty`, `options`, `telemetry get|errors|delete`. Run `coilpro --help` for the list; billed calls are marked there.
