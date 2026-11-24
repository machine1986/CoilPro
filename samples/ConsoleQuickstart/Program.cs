// ConsoleQuickstart - end-to-end CoilPro walkthrough:
//   1. login (authorization code + PKCE, loopback listener on http://localhost:8080/callback)
//   2. list vehicles
//   3. read charge_state / drive_state / location_data
//   4. OPTIONAL: send a harmless signed command (flash lights)
//
// Runs WITHOUT credentials: it then only prints the authorization URL and what it would do.
//
// Environment variables
//   TESLA_CLIENT_ID             Application Client ID (required for a real login)
//   TESLA_CLIENT_SECRET         Client secret (optional)
//   TESLA_REDIRECT_URI          Must be a localhost URI registered on your app
//                               (default: http://localhost:8080/callback)
//   TESLA_REGION                "NorthAmerica" or "Europe" (default: Europe)
//   COILPRO_PRIVATE_KEY_PEM_FILE   Path to the P-256 private key PEM (see `coilpro keys generate`).
//                               Needed for signed commands.
//   COILPRO_SAMPLE_VIN             17-character VIN of the vehicle to flash. Needed for commands.
//
// Signed commands additionally require: the matching public key registered via partner
// registration, and the virtual key paired in the car via https://tesla.com/_ak/<your-domain>.
//
// Run with:  dotnet run --project samples/ConsoleQuickstart
using System.Net;
using System.Text;
using CoilPro;
using CoilPro.Commands;

var clientId = Environment.GetEnvironmentVariable("TESLA_CLIENT_ID") ?? "REPLACE_WITH_YOUR_CLIENT_ID";
var redirectUri = new Uri(Environment.GetEnvironmentVariable("TESLA_REDIRECT_URI") ?? "http://localhost:8080/callback");
var region = Enum.TryParse<TeslaRegion>(Environment.GetEnvironmentVariable("TESLA_REGION"), true, out var r)
    ? r
    : TeslaRegion.Europe;
var keyFile = Environment.GetEnvironmentVariable("COILPRO_PRIVATE_KEY_PEM_FILE");
var vin = Environment.GetEnvironmentVariable("COILPRO_SAMPLE_VIN");
var canSendCommand = !string.IsNullOrWhiteSpace(keyFile) && !string.IsNullOrWhiteSpace(vin);
var hasCredentials = clientId != "REPLACE_WITH_YOUR_CLIENT_ID";

var authOptions = new TeslaAuthOptions
{
    ClientId = clientId,
    ClientSecret = Environment.GetEnvironmentVariable("TESLA_CLIENT_SECRET"),
    RedirectUri = redirectUri,
    Region = region,
    // Request only what you need. Commands need vehicle_cmds (and vehicle_charging_cmds for charging).
    Scopes = TeslaScopes.Openid | TeslaScopes.OfflineAccess | TeslaScopes.VehicleDeviceData
           | TeslaScopes.VehicleLocation | TeslaScopes.VehicleCmds | TeslaScopes.VehicleChargingCmds,
};

using var authHttp = new HttpClient();
ITeslaAuthClient authClient = new TeslaAuthClient(authHttp, authOptions);

// Step 1: build the authorization URL. State and PKCE verifier must be kept until the callback.
var request = authClient.CreateAuthorizationRequest();

Console.WriteLine("CoilPro - ConsoleQuickstart");
Console.WriteLine("============================");
Console.WriteLine();
Console.WriteLine("Authorization URL (opens Tesla's login page):");
Console.WriteLine($"  {request.AuthorizationUrl}");
Console.WriteLine();

if (!hasCredentials)
{
    Console.WriteLine("TESLA_CLIENT_ID is not set - dry run. With credentials this sample would:");
    Console.WriteLine($"  1. listen on {redirectUri} and receive the authorization code");
    Console.WriteLine("  2. exchange the code (PKCE) for tokens");
    Console.WriteLine("  3. list your vehicles");
    Console.WriteLine("  4. read charge_state, drive_state and location_data of the first vehicle");
    Console.WriteLine(canSendCommand
        ? $"  5. flash the lights of {vin} with a signed command"
        : "  5. (only if COILPRO_PRIVATE_KEY_PEM_FILE and COILPRO_SAMPLE_VIN are set) flash the lights with a signed command");
    return;
}

// Step 2: wait for Tesla to redirect the browser back to our loopback listener.
var code = await WaitForCallbackAsync(redirectUri, request.State);
if (code is null)
{
    Console.WriteLine("Login failed or was cancelled.");
    return;
}

// Step 3: exchange the code. The verifier must be the one created for THIS request.
var tokens = await authClient.ExchangeCodeAsync(code, request.Pkce.Verifier);
Console.WriteLine($"Logged in. Access token expires at {tokens.ExpiresAt:u}; refresh token present: {tokens.RefreshToken is not null}");

// The token store + provider keep tokens fresh (refresh on expiry). Use a persistent ITokenStore in real apps.
var store = new InMemoryTokenStore();
await store.SaveAsync(tokens);
var tokenProvider = new TeslaTokenProvider(store, authClient);

var endpoints = authOptions.Endpoints ?? TeslaEndpoints.For(region);
using var apiHttp = new HttpClient { BaseAddress = endpoints.FleetApiBaseUrl };
ICoilProClient client = new CoilProClient(apiHttp, tokenProvider);

// Step 4: list vehicles.
var vehicles = await client.GetVehiclesAsync();
Console.WriteLine();
Console.WriteLine($"Vehicles ({vehicles.Count}):");
foreach (var v in vehicles)
{
    Console.WriteLine($"  {v.Vin}  {v.DisplayName ?? "(unnamed)"}  state={v.State}");
}

// Step 5: read vehicle data. Use the 17-char VIN as the vehicle tag.
var targetVin = vin ?? vehicles.FirstOrDefault()?.Vin;
if (targetVin is null)
{
    Console.WriteLine("No vehicle found.");
    return;
}

try
{
    var data = await client.GetVehicleDataAsync(
        targetVin,
        VehicleDataEndpoint.ChargeState | VehicleDataEndpoint.DriveState | VehicleDataEndpoint.LocationData);
    Console.WriteLine();
    Console.WriteLine($"Data for {targetVin}:");
    Console.WriteLine($"  Battery:  {data.ChargeState?.BatteryLevel}%  ({data.ChargeState?.ChargingState})");
    Console.WriteLine($"  Position: {data.DriveState?.Latitude}, {data.DriveState?.Longitude}");
}
catch (TeslaApiException ex)
{
    // A sleeping car typically answers 408; call client.WakeUpAsync(vin) first if you need fresh data.
    Console.WriteLine($"Reading vehicle data failed: {ex.StatusCode} {ex.Message}");
}

// Step 6 (optional): a signed command. Skipped unless key + VIN are configured.
if (!canSendCommand)
{
    Console.WriteLine();
    Console.WriteLine("Skipping signed command: set COILPRO_PRIVATE_KEY_PEM_FILE and COILPRO_SAMPLE_VIN to flash the lights.");
    return;
}

using var keyPair = TeslaKeyPair.FromPrivateKeyPem(await File.ReadAllTextAsync(keyFile!));
var commands = new CoilProCommands(apiHttp, tokenProvider, keyPair, client, new CoilProCommandsOptions { AutoWake = true });
try
{
    CommandResult result = await commands.FlashLightsAsync(vin!);
    Console.WriteLine();
    Console.WriteLine($"FlashLights -> success={result.Success}, reason={result.Reason ?? "-"}");
}
catch (TeslaKeyNotPairedException ex)
{
    Console.WriteLine($"Key not paired: {ex.Message}");
}
catch (TeslaVehicleAsleepException ex)
{
    Console.WriteLine($"Vehicle asleep/offline: {ex.Message}");
}

// Minimal loopback listener: waits for one request on the redirect URI, validates state, returns the code.
static async Task<string?> WaitForCallbackAsync(Uri redirect, string expectedState)
{
    using var listener = new HttpListener();
    listener.Prefixes.Add($"{redirect.Scheme}://{redirect.Authority}/");
    listener.Start();
    Console.WriteLine($"Open the URL above in your browser; waiting for the redirect on {redirect} ...");

    while (true)
    {
        var ctx = await listener.GetContextAsync();
        if (ctx.Request.Url?.AbsolutePath != redirect.AbsolutePath)
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.Close();
            continue;
        }

        var query = System.Web.HttpUtility.ParseQueryString(ctx.Request.Url.Query);
        var ok = query["code"] is not null && query["state"] == expectedState;
        var body = Encoding.UTF8.GetBytes(ok
            ? "Login complete. You can close this tab."
            : $"Login failed: {query["error"] ?? "invalid state"} {query["error_description"]}");
        ctx.Response.ContentType = "text/plain; charset=utf-8";
        await ctx.Response.OutputStream.WriteAsync(body);
        ctx.Response.Close();
        return ok ? query["code"] : null;
    }
}
