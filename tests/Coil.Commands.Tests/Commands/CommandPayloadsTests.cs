using Coil.Commands.Internal.Commands;
using UniversalMessage;

namespace Coil.Commands.Tests.Commands;

/// <summary>
/// Golden bytes are hand-encoded from the .proto field numbers (tag = field &lt;&lt; 3 | wire type), not produced
/// by the builders. Action.vehicleAction = field 2 (0x12).
/// </summary>
public class CommandPayloadsTests
{
    private static void Check(string expectedHex, Domain domain, string name, VehicleCommand command)
    {
        Assert.Equal(name, command.Name);
        Assert.Equal(domain, command.Domain);
        Assert.Equal(expectedHex, Convert.ToHexString(command.Payload));
    }

    [Fact] public void Lock() => Check("1001", Domain.VehicleSecurity, "lock", CommandPayloads.Lock());

    // RKEAction is a oneof member, so the default value 0 is still emitted.
    [Fact] public void Unlock() => Check("1000", Domain.VehicleSecurity, "unlock", CommandPayloads.Unlock());

    // vehicleControlHonkHornAction = 27 -> tag 0xDA 0x01
    [Fact] public void HonkHorn() => Check("1203DA0100", Domain.Infotainment, "honk_horn", CommandPayloads.HonkHorn());

    // vehicleControlFlashLightsAction = 26 -> tag 0xD2 0x01
    [Fact] public void FlashLights() => Check("1203D20100", Domain.Infotainment, "flash_lights", CommandPayloads.FlashLights());

    // protocol.md HVAC-on plaintext vector
    [Fact] public void StartClimate() => Check("120452020801", Domain.Infotainment, "start_climate", CommandPayloads.StartClimate());

    // hvacAutoAction = 10 (0x52), power_on=false is the proto3 default -> empty message
    [Fact] public void StopClimate() => Check("12025200", Domain.Infotainment, "stop_climate", CommandPayloads.StopClimate());

    // hvacTemperatureAdjustmentAction = 14 (0x72): level(5)={TEMP_MAX(3)} = 2A 02 1A 00,
    // driver(6) fixed32 21.0f = 35 0000A841, passenger(7) fixed32 22.5f = 3D 0000B441
    [Fact]
    public void SetTemperatures() => Check(
        "1210720E2A021A0035" + "0000A841" + "3D" + "0000B441",
        Domain.Infotainment, "set_temperatures", CommandPayloads.SetTemperatures(21.0f, 22.5f));

    // chargingStartStopAction = 6 (0x32): start = 2 (0x12) / stop = 5 (0x2A), empty Void
    [Fact] public void StartCharging() => Check("120432021200", Domain.Infotainment, "start_charging", CommandPayloads.StartCharging());

    [Fact] public void StopCharging() => Check("120432022A00", Domain.Infotainment, "stop_charging", CommandPayloads.StopCharging());

    // chargingSetLimitAction = 5 (0x2A), percent=80 -> 08 50
    [Fact] public void SetChargeLimit() => Check("12042A020850", Domain.Infotainment, "set_charge_limit", CommandPayloads.SetChargeLimit(80));

    // chargePortDoorOpen = 62 -> tag 0xF2 0x03
    [Fact] public void OpenChargePort() => Check("1203F20300", Domain.Infotainment, "open_charge_port", CommandPayloads.OpenChargePort());

    // setChargingAmpsAction = 43 -> tag 0xDA 0x02, charging_amps=16 -> 08 10
    [Fact] public void SetChargingAmps() => Check("1205DA02020810", Domain.Infotainment, "set_charging_amps", CommandPayloads.SetChargingAmps(16));

    [Theory]
    [InlineData(50, "12042A020832")]
    [InlineData(100, "12042A020864")]
    public void ChargeLimitBoundsAccepted(int percent, string hex) =>
        Check(hex, Domain.Infotainment, "set_charge_limit", CommandPayloads.SetChargeLimit(percent));

    [Theory]
    [InlineData(49)]
    [InlineData(101)]
    [InlineData(0)]
    [InlineData(-1)]
    public void ChargeLimitOutOfRangeThrows(int percent) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.SetChargeLimit(percent));

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ChargingAmpsNotPositiveThrows(int amps) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.SetChargingAmps(amps));

    [Fact]
    public void ChargingAmpsHasNoUpperBound() =>
        Assert.Equal(Domain.Infotainment, CommandPayloads.SetChargingAmps(1000).Domain);

    [Theory]
    [InlineData(float.NaN, 20f)]
    [InlineData(20f, float.PositiveInfinity)]
    public void NonFiniteTemperatureThrows(float driver, float passenger) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.SetTemperatures(driver, passenger));

    [Fact]
    public void TemperaturesAreNotRangeChecked() =>
        Assert.Equal(Domain.Infotainment, CommandPayloads.SetTemperatures(5f, 40f).Domain);
}
