// AspNetCoreQuickstart - CoilPro in a minimal API.
//
//   GET  /login                     redirect to Tesla's authorize page
//   GET  /callback                  Tesla redirects back here; exchanges the code, stores tokens
//   GET  /vehicles                  list vehicles
//   GET  /vehicles/{vin}/charge     read charge_state
//   POST /vehicles/{vin}/flash      signed command: flash lights
//
// Configuration (appsettings.json / user-secrets / env vars such as Coil__ClientId):
//   CoilPro:ClientId, CoilPro:ClientSecret, CoilPro:RedirectUri (must be localhost), CoilPro:Region,
//   CoilPro:PrivateKeyPemFile (path) or CoilPro:PrivateKeyPem (inline PEM) for signed commands.
using System.Collections.Concurrent;
using CoilPro;
using CoilPro.AspNetCore;
using CoilPro.Commands;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration.GetSection("CoilPro");

// Reads the private key lazily: the app starts (and /login, /vehicles work) even without a key;
// only the command endpoint needs it.
TeslaKeyPair LoadKeyPair(IServiceProvider _)
{
    var pem = cfg["PrivateKeyPem"];
    var file = cfg["PrivateKeyPemFile"];
    if (string.IsNullOrWhiteSpace(pem) && !string.IsNullOrWhiteSpace(file))
    {
        pem = File.ReadAllText(file);
    }

    return string.IsNullOrWhiteSpace(pem)
        ? throw new InvalidOperationException("Set CoilPro:PrivateKeyPemFile or CoilPro:PrivateKeyPem to send signed commands.")
        : TeslaKeyPair.FromPrivateKeyPem(pem);
}

// Registers ITeslaAuthClient, ITokenStore (in-memory default), ITeslaTokenProvider and ICoilProClient.
builder.Services.AddCoilProApi(o =>
{
    o.ClientId = cfg["ClientId"] ?? throw new InvalidOperationException("CoilPro:ClientId is missing.");
    o.ClientSecret = string.IsNullOrEmpty(cfg["ClientSecret"]) ? null : cfg["ClientSecret"];
    o.RedirectUri = new Uri(cfg["RedirectUri"] ?? "http://localhost:8080/callback");
    o.Region = Enum.TryParse<TeslaRegion>(cfg["Region"], true, out var region) ? region : TeslaRegion.Europe;
    o.Scopes = TeslaScopes.Openid | TeslaScopes.OfflineAccess | TeslaScopes.VehicleDeviceData
             | TeslaScopes.VehicleLocation | TeslaScopes.VehicleCmds | TeslaScopes.VehicleChargingCmds;
});

// Registers ICoilProCommands (signed commands) with the key pair above.
builder.Services.AddCoilProCommands(LoadKeyPair, o => o.AutoWake = true);

// Sample-only: remember the PKCE verifier per login attempt (state -> verifier).
// A real app would keep this in the user's session or a distributed cache.
var pendingLogins = new ConcurrentDictionary<string, string>();

var app = builder.Build();

app.MapGet("/", () => "CoilPro AspNetCoreQuickstart - open /login to sign in with your Tesla account.");

app.MapGet("/login", (ITeslaAuthClient auth) =>
{
    var request = auth.CreateAuthorizationRequest();
    pendingLogins[request.State] = request.Pkce.Verifier;
    return Results.Redirect(request.AuthorizationUrl.ToString());
});

app.MapGet("/callback", async (HttpRequest http, ITeslaAuthClient auth, ITokenStore store, CancellationToken ct) =>
{
    var cb = TeslaOAuthCallback.Parse(http.QueryString.Value);
    if (!cb.IsSuccess)
    {
        return Results.BadRequest(new { error = cb.Error ?? "missing_code", description = cb.ErrorDescription });
    }

    if (cb.State is null || !pendingLogins.TryRemove(cb.State, out var verifier))
    {
        return Results.BadRequest(new { error = "invalid_state" });
    }

    var tokens = await auth.ExchangeCodeAsync(cb.Code!, verifier, ct);
    await store.SaveAsync(tokens, ct); // TeslaTokenProvider reads (and refreshes) from here.
    return Results.Ok(new { loggedIn = true, expiresAt = tokens.ExpiresAt, hasRefreshToken = tokens.RefreshToken is not null });
});

app.MapGet("/vehicles", async (ICoilProClient client, CancellationToken ct) =>
    Results.Ok((await client.GetVehiclesAsync(ct)).Select(v => new { v.Vin, v.DisplayName, v.State })));

app.MapGet("/vehicles/{vin}/charge", async (string vin, ICoilProClient client, CancellationToken ct) =>
{
    var data = await client.GetVehicleDataAsync(vin, VehicleDataEndpoint.ChargeState, ct);
    return Results.Ok(data.ChargeState);
});

app.MapPost("/vehicles/{vin}/flash", async (string vin, ICoilProCommands commands, CancellationToken ct) =>
{
    try
    {
        var result = await commands.FlashLightsAsync(vin, ct);
        // A refusal by the car is a normal outcome, not an HTTP error.
        return Results.Ok(new { success = result.Success, reason = result.Reason });
    }
    catch (TeslaKeyNotPairedException ex)
    {
        return Results.Json(
            new { error = "key_not_paired", message = ex.Message, hint = "Open https://tesla.com/_ak/<your-domain> on your phone and approve the key in the car." },
            statusCode: StatusCodes.Status409Conflict);
    }
    catch (TeslaVehicleAsleepException ex)
    {
        return Results.Json(new { error = "vehicle_asleep", message = ex.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();
