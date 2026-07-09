// ConsoleQuickstart — walks through the Coil.NET authorization-code + PKCE flow end to end.
//
// This sample builds and runs WITHOUT real Tesla credentials: it falls back to safe
// placeholders so you can see the authorization URL shape immediately. To complete a real
// login, register a Tesla developer application at https://developer.tesla.com/en_US/dashboard
// and set these environment variables before running:
//
//   TESLA_CLIENT_ID      Your application's Client ID (required for a real login)
//   TESLA_CLIENT_SECRET  Your application's Client Secret (optional; confidential clients only)
//   TESLA_REDIRECT_URI   A redirect URI registered on the application
//                         (default: http://localhost:8080/callback)
//   TESLA_REGION         "NorthAmerica" or "Europe" (default: Europe)
//
// Run with:  dotnet run --project samples/ConsoleQuickstart
using Coil;

var clientId = Environment.GetEnvironmentVariable("TESLA_CLIENT_ID") ?? "REPLACE_WITH_YOUR_CLIENT_ID";
var clientSecret = Environment.GetEnvironmentVariable("TESLA_CLIENT_SECRET");
var redirectUri = Environment.GetEnvironmentVariable("TESLA_REDIRECT_URI") ?? "http://localhost:8080/callback";
var region = Enum.TryParse<TeslaRegion>(
    Environment.GetEnvironmentVariable("TESLA_REGION"),
    ignoreCase: true,
    out var parsedRegion)
    ? parsedRegion
    : TeslaRegion.Europe;

var options = new TeslaAuthOptions
{
    ClientId = clientId,
    ClientSecret = clientSecret,
    RedirectUri = new Uri(redirectUri),
    Region = region,
    // openid + offline_access give you an id token and a refresh token; vehicle_device_data
    // lets a later call read vehicle state. Combine flags freely — see TeslaScopes.
    Scopes = TeslaScopes.Openid | TeslaScopes.OfflineAccess | TeslaScopes.VehicleDeviceData,
};

// A plain HttpClient is enough here. In an ASP.NET Core app, prefer
// services.AddCoilApi(o => { ... }) (Coil.AspNetCore) for pooled/managed HttpClient instances.
using var http = new HttpClient();
ITeslaAuthClient authClient = new TeslaAuthClient(http, options);

// Step 1: build the authorization URL. Persist State and Pkce (session, cache, whatever fits
// your app) — you need both again in step 3 to complete the exchange.
var authorizationRequest = authClient.CreateAuthorizationRequest();

Console.WriteLine("Coil.NET — ConsoleQuickstart");
Console.WriteLine("=============================");
Console.WriteLine();
Console.WriteLine("Step 1: open this URL in a browser and log in with your Tesla account:");
Console.WriteLine();
Console.WriteLine($"  {authorizationRequest.AuthorizationUrl}");
Console.WriteLine();
Console.WriteLine("Step 2: after granting access, Tesla redirects to your redirect URI with a");
Console.WriteLine("        \"?code=...\" query parameter (and the same \"&state=\" printed above,");
Console.WriteLine("        which you should verify to guard against CSRF).");
Console.WriteLine();

if (clientId == "REPLACE_WITH_YOUR_CLIENT_ID")
{
    Console.WriteLine("No TESLA_CLIENT_ID environment variable was set, so the URL above uses a");
    Console.WriteLine("placeholder client id and will not work against the real Tesla auth server.");
    Console.WriteLine("Set TESLA_CLIENT_ID (and optionally TESLA_CLIENT_SECRET / TESLA_REDIRECT_URI /");
    Console.WriteLine("TESLA_REGION) and run again to try a real login. Skipping the code exchange.");
    return;
}

Console.Write("Step 3: paste the \"code\" query parameter here (or press Enter to skip): ");
var code = Console.ReadLine();

if (string.IsNullOrWhiteSpace(code))
{
    Console.WriteLine("No code entered — exiting without exchanging tokens.");
    return;
}

// Step 3 (continued): exchange the authorization code for tokens. The PKCE code verifier must
// be the one generated for THIS authorization request.
var tokens = await authClient.ExchangeCodeAsync(code.Trim(), authorizationRequest.Pkce.Verifier);

Console.WriteLine();
Console.WriteLine("Exchange succeeded:");
Console.WriteLine($"  Token type:             {tokens.TokenType}");
Console.WriteLine($"  Expires at (UTC):       {tokens.ExpiresAt:u}");
Console.WriteLine($"  Access token (preview): {tokens.AccessToken[..Math.Min(12, tokens.AccessToken.Length)]}...");
Console.WriteLine($"  Refresh token present:  {tokens.RefreshToken is not null}");
Console.WriteLine();
Console.WriteLine("Save the refresh token (e.g. via an ITokenStore) and use TeslaAuthClient.RefreshAsync");
Console.WriteLine("to get a new access token once this one expires — no need to repeat steps 1-3.");
