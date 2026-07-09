# Getting Started

Five minutes from an empty console project to a printed Tesla authorization URL and an exchanged access token. No Tesla account required for the first part — you only need one to actually log in.

## Prerequisites

- .NET SDK 8 or 10 installed (`dotnet --version`)
- Optional, for a real login: a Tesla developer application, registered at the [Tesla developer dashboard](https://developer.tesla.com/en_US/dashboard) (Client ID, and a Client Secret if you registered a confidential client)

> Registering an application and completing partner/domain registration is a separate, more involved process — see [Partner Onboarding](partner-onboarding.md). You don't need any of that to try the flow below with a placeholder client id.

## 1. Install the package

```bash
dotnet new console -n MyTeslaApp
cd MyTeslaApp
dotnet add package Coil
```

## 2. Build the authorization URL

```csharp
using Coil;

var options = new TeslaAuthOptions
{
    ClientId = Environment.GetEnvironmentVariable("TESLA_CLIENT_ID") ?? "REPLACE_WITH_YOUR_CLIENT_ID",
    RedirectUri = new Uri("http://localhost:8080/callback"),
    Region = TeslaRegion.Europe, // or TeslaRegion.NorthAmerica
    Scopes = TeslaScopes.Openid | TeslaScopes.OfflineAccess | TeslaScopes.VehicleDeviceData,
};

using var http = new HttpClient();
ITeslaAuthClient authClient = new TeslaAuthClient(http, options);

// Persist State and Pkce (session, cache, ...) — you need both again after the redirect.
var request = authClient.CreateAuthorizationRequest();
Console.WriteLine($"Open this URL and log in: {request.AuthorizationUrl}");
```

Open the printed URL in a browser, log in with your Tesla account, and grant the requested scopes. Tesla redirects back to your `RedirectUri` with `?code=...&state=...`.

## 3. Exchange the code for tokens

```csharp
// code comes from the "code" query parameter on the redirect; codeVerifier is
// request.Pkce.Verifier from step 2, for the SAME AuthorizationRequest.
var tokens = await authClient.ExchangeCodeAsync(code, request.Pkce.Verifier);

Console.WriteLine($"Access token expires at {tokens.ExpiresAt:u}");
```

`tokens.RefreshToken` is populated when `TeslaScopes.OfflineAccess` was requested (as above). Store it with an `ITokenStore` implementation — `InMemoryTokenStore` is a good default for quick experiments — and call `authClient.RefreshAsync(refreshToken)` once the access token expires, instead of repeating steps 2-3.

## 4. Try it end to end

The repository ships a runnable version of the steps above at [`samples/ConsoleQuickstart`](https://github.com/machine1986/Coil/tree/main/samples/ConsoleQuickstart):

```bash
dotnet run --project samples/ConsoleQuickstart
```

It builds and starts without any environment variables set — it falls back to placeholders and shows you the authorization URL shape. Set `TESLA_CLIENT_ID` (and optionally `TESLA_CLIENT_SECRET`, `TESLA_REDIRECT_URI`, `TESLA_REGION`) to run a real login end to end.

## Using dependency injection (ASP.NET Core)

In an ASP.NET Core app, register Coil.NET with the `Coil.AspNetCore` DI extension instead of constructing `HttpClient`/`TeslaAuthClient` by hand:

```csharp
builder.Services.AddCoilApi(o =>
{
    o.ClientId = builder.Configuration["Tesla:ClientId"]!;
    o.ClientSecret = builder.Configuration["Tesla:ClientSecret"];
    o.RedirectUri = new Uri(builder.Configuration["Tesla:RedirectUri"]!);
    o.Region = TeslaRegion.Europe;
    o.Scopes = TeslaScopes.Openid | TeslaScopes.OfflineAccess | TeslaScopes.VehicleDeviceData;
});
```

This wires up `ITeslaAuthClient` through `IHttpClientFactory` so you get pooled connections and Polly resilience for free; inject `ITeslaAuthClient` wherever you need it.

## Next steps

- Shipping to real users? Walk through [Partner Onboarding](partner-onboarding.md) — keypair generation, `/.well-known` hosting, and Tesla partner registration.
- The `coil` CLI tool (`tools/Coil.Cli`) is being built out to automate parts of onboarding and to run quick smoke tests (`coil login`, `coil vehicles`, `coil wake <vin>`) — see Partner Onboarding for its planned command surface.
