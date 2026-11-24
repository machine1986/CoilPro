# Reading Vehicle Data

`ICoilClient` is the read-access surface for the Tesla Fleet API's vehicle endpoints: list the vehicles on an account, fetch `vehicle_data`, wake a sleeping car, and pull charging history. This article builds on [Getting Started](getting-started.md) — you need a working `ITeslaAuthClient` (or stored tokens) before any of this works. Verified live against the EU region with one vehicle.

> **The Fleet API is pay-per-use.** Every `vehicle_data` call costs money, and cost scales with which data segments you request and whether the vehicle had to be woken up first. The single most important habit in this article is: **request only the segments you need, and never wake a sleeping car just to read data.**

## Prerequisites

- A working OAuth setup from [Getting Started](getting-started.md) (`ITeslaAuthClient`, a `TeslaScopes.VehicleDeviceData` grant).
- Stored tokens behind an `ITokenStore` — `ICoilClient` doesn't do OAuth itself, it only reads a bearer token from an `ITeslaTokenProvider`.

## Building a `CoilClient`

### Via dependency injection (recommended)

`Coil.AspNetCore`'s `AddCoilApi` registers `ICoilClient` and `ITeslaTokenProvider` alongside `ITeslaAuthClient`, so you can inject `ICoilClient` directly wherever you need it:

```csharp
builder.Services.AddCoilApi(o =>
{
    o.ClientId = builder.Configuration["Tesla:ClientId"]!;
    o.ClientSecret = builder.Configuration["Tesla:ClientSecret"];
    o.RedirectUri = new Uri(builder.Configuration["Tesla:RedirectUri"]!);
    o.Region = TeslaRegion.Europe;
    o.Scopes = TeslaScopes.Openid | TeslaScopes.OfflineAccess | TeslaScopes.VehicleDeviceData;
});
```

```csharp
public sealed class FleetStatusService(ICoilClient coil)
{
    public Task<IReadOnlyList<Vehicle>> ListVehiclesAsync(CancellationToken ct) =>
        coil.GetVehiclesAsync(ct);
}
```

Register your own `ITokenStore` (the default is `InMemoryTokenStore`, fine for a single process, not for anything that restarts) before or after calling `AddCoilApi` — it's added with `TryAdd`, so your registration wins either way.

### Standalone (no DI container)

```csharp
using Coil;

ITokenStore tokenStore = new InMemoryTokenStore();
// ... populate tokenStore from a completed login (see Getting Started) ...

using var authHttp = new HttpClient();
ITeslaAuthClient authClient = new TeslaAuthClient(authHttp, authOptions);
ITeslaTokenProvider tokenProvider = new TeslaTokenProvider(tokenStore, authClient);

using var apiHttp = new HttpClient { BaseAddress = TeslaEndpoints.For(TeslaRegion.Europe).FleetApiBaseUrl };
ICoilClient coil = new CoilClient(apiHttp, tokenProvider, new CoilClientOptions());
```

The `HttpClient` passed to `CoilClient` needs its `BaseAddress` set to the Fleet API host for your region (`TeslaEndpoints.For(region).FleetApiBaseUrl`) — `AddCoilApi` does this for you via `IHttpClientFactory`; standalone code has to do it explicitly. `CoilClientOptions` is optional and tunes the retry/backoff pipeline (`MaxRetries`, `BaseRetryDelay`, `UseJitter`, `RespectRetryAfter`); pass `null` to accept the defaults.

## Listing vehicles

```csharp
IReadOnlyList<Vehicle> vehicles = await coil.GetVehiclesAsync();

foreach (var v in vehicles)
{
    Console.WriteLine($"{v.DisplayName} ({v.Vin}) — {v.State}");
}
```

`Vehicle.State` tells you `online`, `asleep`, or `offline` **without** any extra cost or waking the car — check it before deciding whether a `vehicle_data` call is worth making.

## Fetching `vehicle_data` — with a cost-conscious filter

`GetVehicleDataAsync` takes a `VehicleDataEndpoint` flags filter. Passing `VehicleDataEndpoint.None` (the default) lets the API return its full default payload — request specific segments instead to keep both the response size and the cost down:

```csharp
var vehicle = vehicles[0];

var data = await coil.GetVehicleDataAsync(
    vehicle.Vin,
    VehicleDataEndpoint.ChargeState | VehicleDataEndpoint.DriveState | VehicleDataEndpoint.ClimateState);
```

`VehicleDataEndpoint` is a `[Flags]` enum: `ChargeState`, `ClimateState`, `DriveState`, `GuiSettings`, `VehicleState`, `VehicleConfig`, `LocationData`, `ClosuresState`. Combine with `|`. `.ToQueryValue()` turns the combination into the semicolon-separated string the `endpoints` query parameter expects (e.g. `"charge_state;drive_state;climate_state"`) — you won't normally call it yourself, `CoilClient` does it internally, but it's public if you need to inspect or log the resulting query value.

Location fields (`latitude`/`longitude` in `drive_state`) are only returned when `VehicleDataEndpoint.LocationData` is requested as well and the token has the `vehicle_location` scope.

Only sub-states you asked for (or that the API includes by default) are populated — everything else is `null`:

```csharp
if (data.ChargeState is { } charge)
{
    Console.WriteLine($"Battery: {charge.BatteryLevel}% ({charge.ChargingState})");
}

if (data.DriveState is { } drive)
{
    Console.WriteLine($"Shift state: {drive.ShiftState}, speed: {drive.Speed}");
}

if (data.ClimateState is { } climate)
{
    Console.WriteLine($"Inside/outside: {climate.InsideTemp}° / {climate.OutsideTemp}°");
}

// Odometer lives on VehicleState — request VehicleDataEndpoint.VehicleState to get it.
if (data.VehicleState is { } vs)
{
    Console.WriteLine($"Odometer: {vs.Odometer}");
}
```

Fields Coil.NET doesn't model explicitly are never dropped: every state record (and `VehicleData` itself) carries a `[JsonExtensionData] AdditionalData` dictionary, so deserialization tolerates new or renamed fields Tesla adds without a SDK update.

### Don't wake sleeping cars just to read data

`vehicle_data` requires an awake vehicle — a request against an `asleep` car fails instead of waking it. `WakeUpAsync` does wake it, which costs the vehicle battery and, on the Fleet API, money. Check `Vehicle.State` first, and only call `WakeUpAsync` when you actually need fresh data badly enough to justify it:

```csharp
if (vehicle.State == "asleep")
{
    Console.WriteLine("Vehicle is asleep — skipping vehicle_data to avoid an unnecessary wake.");
}
else
{
    var fresh = await coil.GetVehicleDataAsync(vehicle.Vin, VehicleDataEndpoint.ChargeState);
}

// Only when you deliberately want to wake it:
// var awake = await coil.WakeUpAsync(vehicle.Vin);
```

## Handling errors

Every non-success response raises `TeslaApiException`, carrying the HTTP status, Tesla's error code (when present), any `Retry-After` hint, the raw response body, and a `RateLimitInfo` snapshot from that failing response:

```csharp
try
{
    var data = await coil.GetVehicleDataAsync(vehicle.Vin, VehicleDataEndpoint.ChargeState);
}
catch (TeslaApiException ex)
{
    Console.Error.WriteLine($"Tesla API error {(int)ex.StatusCode} ({ex.ErrorCode}): {ex.Message}");

    if (ex.RetryAfter is { } retryAfter)
    {
        Console.Error.WriteLine($"Retry after {retryAfter}.");
    }
}
```

`CoilClientOptions` already retries transient failures (408/429/5xx) with backoff before this exception surfaces — `TeslaApiException` means the request ultimately failed after those retries were exhausted (or the error wasn't retryable, e.g. 401/404).

## Watching your rate-limit budget

`ICoilClient.LastRateLimit` holds the `RateLimitInfo` parsed from the most recent response's headers — `null` if no relevant headers were present. Tesla reports one limit/remaining/reset triple per bucket (e.g. `ratelimit-device-data-per-vehicle-*`); `RateLimitInfo` exposes the bucket as `Category` and picks the one with the fewest remaining requests. Surface it to anyone who needs to know how much budget is left before the window resets:

```csharp
_ = await coil.GetVehiclesAsync();

if (coil.LastRateLimit is { } rateLimit)
{
    Console.WriteLine($"Rate limit: {rateLimit.Remaining}/{rateLimit.Limit} remaining, resets at {rateLimit.ResetsAt:u}");
}
```

`RateLimitInfo` is also attached to `TeslaApiException.RateLimit` on a failing (e.g. 429) response, so you can inspect it even when the call itself threw.

## The `coil` CLI

For quick smoke tests without writing any code, `tools/Coil.Cli` (installed with `dotnet tool install --global Coil.Cli`) ships two read commands built on `ICoilClient`, alongside `coil login`:

```bash
coil login              # OAuth flow via a local loopback redirect; tokens are persisted to ~/.coil/
coil vehicles           # list vehicles on the account (Vehicle.Id, Vin, DisplayName, State)
coil data <vin> --endpoints charge_state,drive_state   # vehicle_data, filtered to the given segments
```

`coil data` without `--endpoints` requests `charge_state`, `drive_state`, `climate_state` and `vehicle_state` — pass `--endpoints` with a comma-separated list of segment names (matching the `VehicleDataEndpoint` members: `charge_state`, `climate_state`, `drive_state`, `gui_settings`, `vehicle_state`, `vehicle_config`, `location_data`, `closures_state`) to keep calls cheap and fast, exactly as you would with the SDK's flags enum. Check `coil --help` / `coil data --help` for the exact flags shipped in your installed version.

## Next steps

- Sending commands (charge limit, lock, climate) needs the Vehicle Command Protocol and Virtual Key pairing — see [Signed Vehicle Commands](commands.md) and [Partner Onboarding](partner-onboarding.md).
- Where the live API differed from the planning spec (the `;` separator in `endpoints`, rate-limit header names, the charging-history response shape) the corrections are recorded in [`docs/api-deviations.md`](../api-deviations.md).
