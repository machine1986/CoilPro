# AspNetCoreQuickstart

Minimal API showing Coil.NET with `AddCoilApi` + `AddCoilCommands`. Unofficial; not affiliated with Tesla, Inc.

## Prerequisites

- A Tesla developer application whose redirect URI is `http://localhost:8080/callback` (Tesla only accepts `localhost` redirects).
- For the flash endpoint: a P-256 private key (`coil keys generate`), its public key registered via partner
  registration, and the virtual key paired in the car via `https://tesla.com/_ak/<your-domain>`.

## Configure

Keep secrets out of `appsettings.json`; use user-secrets:

```bash
cd samples/AspNetCoreQuickstart
dotnet user-secrets set "Coil:ClientId" "<client id>"
dotnet user-secrets set "Coil:ClientSecret" "<client secret>"      # optional
dotnet user-secrets set "Coil:PrivateKeyPemFile" "/path/to/private-key.pem"
```

| Key | Meaning |
|---|---|
| `Coil:ClientId` | Application client id (required) |
| `Coil:ClientSecret` | Client secret (optional) |
| `Coil:RedirectUri` | Registered redirect URI, default `http://localhost:8080/callback` |
| `Coil:Region` | `Europe` or `NorthAmerica` |
| `Coil:PrivateKeyPemFile` / `Coil:PrivateKeyPem` | Private key for signed commands |

Environment variables work too (`Coil__ClientId=...`).

## Run

```bash
dotnet run --project samples/AspNetCoreQuickstart
```

1. Open `http://localhost:8080/login` and sign in at Tesla.
2. `GET /vehicles` lists your vehicles (use the 17-character VIN below).
3. `GET /vehicles/{vin}/charge` returns the charge state.
4. `curl -X POST http://localhost:8080/vehicles/{vin}/flash` sends a signed command.

Responses of the flash endpoint: `200 { success, reason }` (a refusal is `success:false`), `409` if the key is not
paired (with the pairing hint), `503` if the vehicle is asleep or offline.

## Notes

Tokens live in the default in-memory `ITokenStore` (single user, lost on restart) and the PKCE verifier in a
dictionary. A real app should register its own `ITokenStore` and keep login state in the user's session.
