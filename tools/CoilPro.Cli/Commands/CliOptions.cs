namespace CoilPro.Cli.Commands;

/// <summary>Thrown for malformed CLI arguments; caught by the command and reported as a usage error.</summary>
internal sealed class CliUsageException(string message) : Exception(message);

/// <summary>
/// Parsed options for the credential-taking commands (<c>coilpro login</c>, <c>coilpro partner</c>):
/// CLI flags override environment variables.
/// </summary>
internal sealed class CliOptions
{
    public string? ClientId { get; private init; }

    public string? ClientSecret { get; private init; }

    /// <summary>Application domain hosting the public key; used by <c>coilpro partner</c>.</summary>
    public string? Domain { get; private init; }

    public TeslaRegion Region { get; private init; } = TeslaRegion.Europe;

    /// <summary>
    /// Scopes for <c>coilpro login</c> when <c>--scopes</c> is not given: everything the vehicle commands of this
    /// CLI need (read data incl. location, send commands). Bare <c>openid offline_access</c> logs in fine but
    /// every vehicle call then fails with "missing scopes". Includes the energy scopes so `coilpro energy` works
    /// out of the box (narrow with <c>--scopes</c> if your application is not configured for them).
    /// </summary>
    public static readonly TeslaScopes DefaultLoginScopes =
        TeslaScopes.Openid | TeslaScopes.OfflineAccess | TeslaScopes.UserData
        | TeslaScopes.VehicleDeviceData | TeslaScopes.VehicleLocation
        | TeslaScopes.VehicleCmds | TeslaScopes.VehicleChargingCmds
        | TeslaScopes.EnergyDeviceData | TeslaScopes.EnergyCmds;

    public TeslaScopes Scopes { get; private init; } = DefaultLoginScopes;

    /// <summary>Fixed loopback port, or 0 to pick a free ephemeral port.</summary>
    public int Port { get; private init; }

    public TimeSpan Timeout { get; private init; } = TimeSpan.FromMinutes(2);

    public static CliOptions Parse(string[] args)
    {
        string? clientId = Environment.GetEnvironmentVariable("COILPRO_CLIENT_ID");
        string? clientSecret = Environment.GetEnvironmentVariable("COILPRO_CLIENT_SECRET");
        string? domain = Environment.GetEnvironmentVariable("COILPRO_DOMAIN");
        var region = ParseRegion(Environment.GetEnvironmentVariable("COILPRO_REGION")) ?? TeslaRegion.Europe;
        TeslaScopes? scopes = null;
        var port = 0;
        var timeout = TimeSpan.FromMinutes(2);

        var i = 0;
        while (i < args.Length)
        {
            var (name, inlineValue) = SplitFlag(args[i]);

            string NextValue()
            {
                if (inlineValue is not null)
                {
                    return inlineValue;
                }

                if (i + 1 >= args.Length)
                {
                    throw new CliUsageException($"Missing value for {name}.");
                }

                i++;
                return args[i];
            }

            switch (name)
            {
                case "--client-id":
                    clientId = NextValue();
                    break;
                case "--client-secret":
                    clientSecret = NextValue();
                    break;
                case "--domain":
                    domain = NextValue();
                    break;
                case "--region":
                    var regionValue = NextValue();
                    region = ParseRegion(regionValue) ?? throw new CliUsageException($"Invalid --region '{regionValue}' (use 'eu' or 'na').");
                    break;
                case "--scopes":
                    scopes = ParseScopes(NextValue());
                    break;
                case "--port":
                    var portValue = NextValue();
                    port = int.TryParse(portValue, out var p) && p >= 0
                        ? p
                        : throw new CliUsageException($"Invalid --port '{portValue}'.");
                    break;
                case "--timeout":
                    var timeoutValue = NextValue();
                    timeout = int.TryParse(timeoutValue, out var s) && s > 0
                        ? TimeSpan.FromSeconds(s)
                        : throw new CliUsageException($"Invalid --timeout '{timeoutValue}'.");
                    break;
                default:
                    throw new CliUsageException($"Unknown option: {args[i]}");
            }

            i++;
        }

        return new CliOptions
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            Domain = domain,
            Region = region,
            Scopes = scopes ?? DefaultLoginScopes,
            Port = port,
            Timeout = timeout,
        };
    }

    private static (string Name, string? InlineValue) SplitFlag(string arg)
    {
        var eq = arg.IndexOf('=');
        return eq < 0 ? (arg, null) : (arg[..eq], arg[(eq + 1)..]);
    }

    private static TeslaRegion? ParseRegion(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" => null,
        "eu" or "europe" => TeslaRegion.Europe,
        "na" or "northamerica" or "us" => TeslaRegion.NorthAmerica,
        _ => null,
    };

    private static TeslaScopes ParseScopes(string value)
    {
        var result = TeslaScopes.None;

        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            result |= part.Replace("_", string.Empty).ToLowerInvariant() switch
            {
                "openid" => TeslaScopes.Openid,
                "offlineaccess" => TeslaScopes.OfflineAccess,
                "userdata" => TeslaScopes.UserData,
                "vehicledevicedata" => TeslaScopes.VehicleDeviceData,
                "vehiclelocation" => TeslaScopes.VehicleLocation,
                "vehiclecmds" => TeslaScopes.VehicleCmds,
                "vehiclechargingcmds" => TeslaScopes.VehicleChargingCmds,
                "energydevicedata" => TeslaScopes.EnergyDeviceData,
                "energycmds" => TeslaScopes.EnergyCmds,
                "vehiclespecs" => TeslaScopes.VehicleSpecs,
                "vehiclepricinginfo" => TeslaScopes.VehiclePricingInfo,
                "enterprisemanagement" => TeslaScopes.EnterpriseManagement,
                _ => throw new CliUsageException($"Unknown scope: '{part}'."),
            };
        }

        return result;
    }
}
