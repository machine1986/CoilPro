using System.Text;
using CoilPro.Commands.Internal.Commands;
using UniversalMessage;

namespace CoilPro.Commands.Tests.Commands;

/// <summary>Reply payloads are hand-encoded from the .proto field numbers.</summary>
public class ReplyInterpreterTests
{
    private static byte[] Hex(string hex) => Convert.FromHexString(hex);

    [Fact]
    public void InfotainmentOk()
    {
        // Response{ actionStatus{} }
        var result = ReplyInterpreter.Interpret(Domain.Infotainment, Hex("0A00"));
        Assert.True(result.Success);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void InfotainmentEmptyPayloadIsSuccess() =>
        Assert.True(ReplyInterpreter.Interpret(Domain.Infotainment, ReadOnlySpan<byte>.Empty).Success);

    [Fact]
    public void InfotainmentErrorWithReason()
    {
        // Response{ actionStatus{ result=ERROR(1), result_reason{ plain_text="already_set" } } }
        var reason = "0A0B" + Convert.ToHexString(Encoding.ASCII.GetBytes("already_set"));
        var status = "0801" + "120D" + reason;
        var result = ReplyInterpreter.Interpret(Domain.Infotainment, Hex("0A11" + status));
        Assert.False(result.Success);
        Assert.Equal("already_set", result.Reason);
    }

    [Fact]
    public void InfotainmentErrorWithoutReason()
    {
        var result = ReplyInterpreter.Interpret(Domain.Infotainment, Hex("0A020801"));
        Assert.False(result.Success);
        Assert.Equal("unspecified error", result.Reason);
    }

    [Fact]
    public void VcsecEmptyIsSuccess() =>
        Assert.True(ReplyInterpreter.Interpret(Domain.VehicleSecurity, ReadOnlySpan<byte>.Empty).Success);

    [Fact]
    public void VcsecCommandStatusOkIsSuccess() =>
        Assert.True(ReplyInterpreter.Interpret(Domain.VehicleSecurity, Hex("2200")).Success);

    [Fact]
    public void VcsecNominalErrorIsRefusedWithEnumName()
    {
        // nominalError(46) = F2 02, genericError = VEHICLE_NOT_IN_PARK (5)
        var result = ReplyInterpreter.Interpret(Domain.VehicleSecurity, Hex("F202020805"));
        Assert.False(result.Success);
        Assert.Equal("VEHICLE_NOT_IN_PARK", result.Reason);
    }

    [Fact]
    public void VcsecWaitThrowsBusy()
    {
        // commandStatus(4){ operationStatus = WAIT(1) }
        var ex = Assert.Throws<TeslaCommandException>(() => ReplyInterpreter.Interpret(Domain.VehicleSecurity, Hex("22020801")));
        Assert.Equal(TeslaMessageFault.Busy, ex.Fault);
    }

    [Fact]
    public void VcsecErrorThrowsWithDetail()
    {
        // commandStatus{ operationStatus=ERROR(2), signedMessageStatus{ counter=5, info=FAULT_NOT_ON_WHITELIST(2) } }
        var ex = Assert.Throws<TeslaCommandException>(
            () => ReplyInterpreter.Interpret(Domain.VehicleSecurity, Hex("22080802" + "12040805" + "1002")));
        Assert.Contains("FAULT_NOT_ON_WHITELIST", ex.Message);
        Assert.Null(ex.Fault);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GarbageThrowsWithoutFault(bool infotainment)
    {
        var ex = Assert.Throws<TeslaCommandException>(() => ReplyInterpreter.Interpret(infotainment ? Domain.Infotainment : Domain.VehicleSecurity, Hex("FFFFFFFF")));
        Assert.Null(ex.Fault);
    }
}
