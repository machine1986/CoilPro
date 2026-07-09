namespace Coil.Tests;

public class TeslaScopesExtensionsTests
{
    [Fact]
    public void ToScopeString_None_ReturnsEmptyString()
    {
        var result = TeslaScopes.None.ToScopeString();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToScopeString_SingleFlag_ReturnsSnakeCaseToken()
    {
        var result = TeslaScopes.VehicleDeviceData.ToScopeString();

        Assert.Equal("vehicle_device_data", result);
    }

    [Fact]
    public void ToScopeString_CommonAuthCodeCombination_ReturnsSpaceSeparatedSnakeCase()
    {
        var scopes = TeslaScopes.Openid | TeslaScopes.OfflineAccess | TeslaScopes.VehicleDeviceData;

        var result = scopes.ToScopeString();

        Assert.Equal("openid offline_access vehicle_device_data", result);
    }

    [Fact]
    public void ToScopeString_EnergyOnlyCombination_ReturnsExpectedString()
    {
        var scopes = TeslaScopes.EnergyDeviceData | TeslaScopes.EnergyCmds;

        var result = scopes.ToScopeString();

        Assert.Equal("energy_device_data energy_cmds", result);
    }

    [Fact]
    public void ToScopeString_AllFlags_ReturnsAllTokensInDeclarationOrder()
    {
        const TeslaScopes all =
            TeslaScopes.Openid | TeslaScopes.OfflineAccess | TeslaScopes.UserData |
            TeslaScopes.VehicleDeviceData | TeslaScopes.VehicleLocation | TeslaScopes.VehicleCmds |
            TeslaScopes.VehicleChargingCmds | TeslaScopes.EnergyDeviceData | TeslaScopes.EnergyCmds;

        var result = all.ToScopeString();

        Assert.Equal(
            "openid offline_access user_data vehicle_device_data vehicle_location vehicle_cmds vehicle_charging_cmds energy_device_data energy_cmds",
            result);
    }
}
