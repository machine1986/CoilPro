# Fleet Telemetry Configuration

Fleet Telemetry lets a vehicle stream selected data fields to a server you run, instead of you polling `vehicle_data`. CoilPro handles the **configuration** side: it signs a telemetry configuration natively and sends it to Tesla, and it can read and delete configurations.

> **Unofficial — not affiliated with Tesla, Inc.**

> **Not included: the telemetry server.** The server that receives the vehicle's stream (Tesla's open-source `fleet-telemetry` project, or your own) is outside CoilPro. You supply its hostname, port and certificate chain.

> **Status (0.2.0).** Reading configurations and errors is verified live (EU, one Model Y, no configuration set). Signing is a native port of what Tesla's Go proxy does and is covered by tests; a configuration has not yet been applied to a real vehicle.

## Prerequisites

- Partner registration and a hosted public key ([Partner Onboarding](partner-onboarding.md)).
- The virtual key installed on the vehicle (`https://tesla.com/_ak/<your-domain>`). Check with `ICoilProClient.GetFleetStatusAsync` or `coilpro status <VIN>`.
- User token with `vehicle_device_data` and `vehicle_location`.
- A running telemetry server whose hostname shares the root domain of your application. Vehicles must support Fleet Telemetry (`FleetStatus` reports the firmware and telemetry version).

## Configure

```csharp
using CoilPro;
using CoilPro.Commands;

IFleetTelemetryConfigurator configurator =
    new FleetTelemetryConfigurator(apiHttp, tokenProvider, keyPair);

var config = new FleetTelemetryConfig
{
    Hostname = "telemetry.example.com",
    Port = 443,
    Ca = File.ReadAllText("ca-chain.pem"),
    Fields = new Dictionary<string, FleetTelemetryFieldConfig>
    {
        ["BatteryLevel"] = new() { IntervalSeconds = 60 },
        ["VehicleSpeed"] = new() { IntervalSeconds = 10, MinimumDelta = 2 },
    },
    AlertTypes = ["service"],
};

FleetTelemetryConfigResult result = await configurator.ConfigureAsync([vin], config);
Console.WriteLine($"Updated: {result.UpdatedVehicles}");
```

Field names are those of Tesla's `vehicle_data.proto` in the `fleet-telemetry` repository. `ConfigureAsync` accepts up to 100 VINs, though Tesla recommends one at a time. `result.SkippedVehicles` lists vehicles Tesla skipped and why (missing key, unsupported hardware or firmware, limit of configurations reached).

The request is sent once and never retried. The signature is a Schnorr/P-256 JWS (`Tesla.SS256`) over the configuration, made with your application key.

With ASP.NET Core, `AddCoilProCommands` registers `IFleetTelemetryConfigurator` as well (after `AddCoilProApi`, with the `TeslaKeyPair`).

## Read and delete

These calls are on `ICoilProClient` and need no signing:

```csharp
FleetTelemetryConfigStatus status = await coil.GetFleetTelemetryConfigAsync(vin);
// status.Config is null when the vehicle has none; status.Synced tells whether the car has applied it.

IReadOnlyList<FleetTelemetryError> errors = await coil.GetFleetTelemetryErrorsAsync(vin);

int updated = await coil.DeleteFleetTelemetryConfigAsync(vin);
```

Across all vehicles of your application, `ITeslaPartnerClient.GetFleetTelemetryErrorsAsync` and `GetFleetTelemetryErrorVinsAsync` show which vehicles have trouble reaching your server.

## CLI

```bash
coilpro telemetry get <VIN>
coilpro telemetry errors <VIN>
coilpro telemetry delete <VIN>
```

Creating a configuration is done from code, because it needs a server certificate chain and a field list.
