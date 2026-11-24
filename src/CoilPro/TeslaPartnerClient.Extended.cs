namespace CoilPro;

public sealed partial class TeslaPartnerClient
{
    /// <inheritdoc />
    public Task<VehicleSpecs> GetVehicleSpecsAsync(string vin, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): GetVehicleSpecsAsync");

    /// <inheritdoc />
    public Task<VehiclePricing> GetVehiclePricingAsync(string market, string model, string language, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): GetVehiclePricingAsync");

    /// <inheritdoc />
    public Task<IReadOnlyList<FleetTelemetryError>> GetFleetTelemetryErrorsAsync(CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): GetFleetTelemetryErrorsAsync");

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetFleetTelemetryErrorVinsAsync(CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): GetFleetTelemetryErrorVinsAsync");
}
