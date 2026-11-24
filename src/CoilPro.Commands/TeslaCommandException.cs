using System.Net;

namespace CoilPro.Commands;

/// <summary>
/// A signed vehicle command could not be delivered or authenticated: protocol fault, transport error,
/// unexpected reply. A vehicle that receives the command and <em>refuses</em> it (e.g. <c>already_set</c>)
/// is not an exception — see <see cref="CommandResult"/>.
/// </summary>
public class TeslaCommandException : Exception
{
    /// <summary>Creates the exception.</summary>
    public TeslaCommandException(
        string message,
        string? vin = null,
        TeslaMessageFault? fault = null,
        HttpStatusCode? statusCode = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Vin = vin;
        Fault = fault;
        StatusCode = statusCode;
    }

    /// <summary>The VIN the command was addressed to, if known.</summary>
    public string? Vin { get; }

    /// <summary>The protocol fault reported by the vehicle, if the failure came from the vehicle.</summary>
    public TeslaMessageFault? Fault { get; }

    /// <summary>The HTTP status of the <c>signed_command</c> call, if the failure came from the Fleet API.</summary>
    public HttpStatusCode? StatusCode { get; }
}

/// <summary>
/// The vehicle does not know CoilPro's public key (fault <c>UNKNOWN_KEY_ID</c> or session status
/// <c>KEY_NOT_ON_WHITELIST</c>). The driver has to install the virtual key by opening
/// <c>https://tesla.com/_ak/&lt;your-domain&gt;</c> in the Tesla app.
/// </summary>
public sealed class TeslaKeyNotPairedException : TeslaCommandException
{
    /// <summary>Creates the exception.</summary>
    public TeslaKeyNotPairedException(string vin, TeslaMessageFault? fault = null)
        : base(
            $"Vehicle {vin} does not know this application's key. Install the virtual key by opening https://tesla.com/_ak/<your-domain> in the Tesla app.",
            vin,
            fault)
    {
    }
}

/// <summary>
/// The vehicle is asleep or offline (HTTP 503, or 408 "vehicle is offline"). Wake it with
/// <c>ICoilProClient.WakeUpAsync</c>, or enable <see cref="CoilProCommandsOptions.AutoWake"/>.
/// </summary>
public sealed class TeslaVehicleAsleepException : TeslaCommandException
{
    /// <summary>Creates the exception.</summary>
    public TeslaVehicleAsleepException(string vin, HttpStatusCode? statusCode = null, string? detail = null)
        : base(
            detail is null ? $"Vehicle {vin} is asleep or offline." : $"Vehicle {vin} is asleep or offline: {detail}",
            vin,
            statusCode: statusCode)
    {
    }
}
