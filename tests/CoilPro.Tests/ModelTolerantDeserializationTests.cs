using System.Text.Json;

namespace CoilPro.Tests;

public class ModelTolerantDeserializationTests
{
    [Fact]
    public void Vehicle_WithUnknownFields_PreservesThemInAdditionalData_AndDefaultsMissingOptionalFields()
    {
        const string json = """{ "id": 1, "vehicle_id": 2, "vin": "VIN123", "some_new_field": 42, "nested": { "a": 1 } }""";

        var vehicle = JsonSerializer.Deserialize<Vehicle>(json)!;

        Assert.Equal(1, vehicle.Id);
        Assert.Equal(2, vehicle.VehicleId);
        Assert.Equal("VIN123", vehicle.Vin);
        Assert.Null(vehicle.DisplayName);
        Assert.Null(vehicle.State);

        Assert.NotNull(vehicle.AdditionalData);
        Assert.Equal(42, vehicle.AdditionalData!["some_new_field"].GetInt32());
        Assert.Equal(JsonValueKind.Object, vehicle.AdditionalData["nested"].ValueKind);
    }

    [Fact]
    public void Vehicle_WithNoUnknownFields_HasEmptyOrNullAdditionalData()
    {
        const string json = """{ "id": 1, "vehicle_id": 2, "vin": "VIN123" }""";

        var vehicle = JsonSerializer.Deserialize<Vehicle>(json)!;

        Assert.True(vehicle.AdditionalData is null || vehicle.AdditionalData.Count == 0);
    }

    [Fact]
    public void VehicleData_WithOnlyChargeStatePresent_LeavesOtherSubStatesNull()
    {
        const string json = """{ "id": 1, "charge_state": { "battery_level": 55 } }""";

        var data = JsonSerializer.Deserialize<VehicleData>(json)!;

        Assert.NotNull(data.ChargeState);
        Assert.Equal(55, data.ChargeState!.BatteryLevel);
        Assert.Null(data.DriveState);
        Assert.Null(data.ClimateState);
        Assert.Null(data.VehicleState);
        Assert.Null(data.GuiSettings);
    }

    [Fact]
    public void ChargeState_WithMissingOptionalNumericFields_LeavesThemNull()
    {
        const string json = """{ "charging_state": "Disconnected" }""";

        var chargeState = JsonSerializer.Deserialize<ChargeState>(json)!;

        Assert.Equal("Disconnected", chargeState.ChargingState);
        Assert.Null(chargeState.BatteryLevel);
        Assert.Null(chargeState.ChargerPower);
        Assert.Null(chargeState.MinutesToFullCharge);
    }

    [Fact]
    public void ChargingRecord_WithUnknownFields_PreservesThemInAdditionalData()
    {
        const string json = """{ "vin": "VIN1", "unknownField": "x" }""";

        var record = JsonSerializer.Deserialize<ChargingRecord>(json)!;

        Assert.Equal("VIN1", record.Vin);
        Assert.NotNull(record.AdditionalData);
        Assert.Equal("x", record.AdditionalData!["unknownField"].GetString());
    }

    [Fact]
    public void ChargingHistoryPage_WithMissingPagingMetadata_LeavesThemNull_AndDataDefaultsToEmpty()
    {
        const string json = """{ "data": [] }""";

        var page = JsonSerializer.Deserialize<ChargingHistoryPage>(json)!;

        Assert.Empty(page.Data);
        Assert.Null(page.TotalResults);
        Assert.Null(page.HasMoreData);
    }
}
