# Partner Onboarding

This is the part of integrating with the Tesla Fleet API that trips up almost every third-party developer: registering an application, hosting a public key, and pairing a Virtual Key in the vehicle before a single signed command can be sent. Coil.NET's goal is to make this a guided, well-documented process instead of a scavenger hunt through forum threads.

All steps are implemented. Steps 1–5 and the signed-command path were verified live on the EU region with one Model Y (EU registration, partner-token scopes, login, `fleet_status`, `flash`, `charge-limit`, `lock`/`unlock`); the North America region has not been exercised. All endpoint paths and requirements below were verified against [developer.tesla.com](https://developer.tesla.com) and [`teslamotors/vehicle-command`](https://github.com/teslamotors/vehicle-command) on 2026-07-28; confirmed deviations from the planning spec are recorded in [`docs/api-deviations.md`](../api-deviations.md). Tesla changes these without much notice — re-verify if something below stops matching reality.

## Overview

| # | Step | Needed for | Status |
|---|---|---|---|
| 1 | Register a Tesla developer application | OAuth login, partner token | Available |
| 2 | Generate an EC P-256 keypair | Domain verification, signing commands | Available — `coil keys generate` |
| 3 | Host the public key at `/.well-known/appspecific/com.tesla.3p.public-key.pem` | Domain verification | Available — static hosting |
| 4 | Register your domain with Tesla | Domain verification | Available — `coil partner register` |
| 5 | Verify what Tesla stored | Catching a bad registration early | Available — `coil partner public-key` |
| 6 | Pair a Virtual Key in the vehicle (deep link) | Sending signed commands | Manual step in the Tesla app |
| 7 | Send signed commands (`Coil.Commands`) | Vehicle Command Protocol | Available — `Coil.Commands`, `coil cmd` |

## The short version

```bash
coil keys generate
# publish keys/public-key.pem at https://<domain>/.well-known/appspecific/com.tesla.3p.public-key.pem
coil partner register   --domain <domain> --client-id <id> --client-secret <secret> --region eu
coil partner public-key --domain <domain> --client-id <id> --client-secret <secret> --region eu
coil login --client-id <id> --client-secret <secret> --region eu --port 8080
coil vehicles
coil cmd <VIN> flash --wake   # needs the virtual key from step 6
```

## Step 1 — Register a Tesla developer application

Create an application at the [Tesla developer dashboard](https://developer.tesla.com/en_US/dashboard). This gives you a Client ID and Client Secret and lets you configure redirect URIs and **Allowed Origins** — everything `TeslaAuthOptions` needs for the authorization-code flow described in [Getting Started](getting-started.md).

Note the Allowed Origins you configure here: the domain you register in step 4 must match one of their root domains.

## Step 2 — Generate an EC P-256 keypair

The Vehicle Command Protocol signs every command with an EC P-256 (`secp256r1` / `prime256v1`) keypair. The private key never leaves your infrastructure; only the public key is published and registered.

```bash
coil keys generate
```

This writes an owner-readable private key and a public key to `~/.coil/keys/` and prints the next steps. `coil keys generate` refuses to overwrite an existing private key unless you pass `--force` — losing a signing key means re-pairing every vehicle.

The equivalent with openssl, if you would rather generate the key yourself:

```bash
openssl ecparam -name prime256v1 -genkey -noout -out private-key.pem
openssl ec -in private-key.pem -pubout -out public-key.pem
```

Coil.NET reads both PKCS#8 (`BEGIN PRIVATE KEY`) and SEC 1 (`BEGIN EC PRIVATE KEY`) PEMs, so either form loads without conversion.

> **Watch the curve.** Tesla's own `vehicle-command` README shows an `openssl req` example using **secp384r1** — that key is for the HTTP proxy's TLS certificate, *not* for command signing. A P-384 key here fails later in a way that is hard to diagnose, so `TeslaKeyPair` rejects anything that is not P-256 up front.

## Step 3 — Host the public key

The public key must be reachable, unauthenticated, over HTTPS at exactly:

```
https://<your-domain>/.well-known/appspecific/com.tesla.3p.public-key.pem
```

This is plain static file hosting — no Coil.NET dependency. Serve the file byte-for-byte as generated (`coil keys show` prints the exact content), with a content type of `application/x-pem-file` or `text/plain`, and make sure the URL does not redirect to a login page or a CDN error page.

Anything that serves static files works: an nginx location block, an S3 bucket behind CloudFront, GitHub Pages, or Cloudflare Pages.

Check it before registering:

```bash
curl -fsS https://<your-domain>/.well-known/appspecific/com.tesla.3p.public-key.pem
```

## Step 4 — Register your domain with Tesla

Registration is authenticated with a **partner token** (OAuth client-credentials), not a user token, so this command needs the client secret. Coil.NET never persists the secret — pass it per invocation or via `COIL_CLIENT_SECRET`.

```bash
coil partner register --domain <your-domain> --client-id <id> --client-secret <secret> --region eu
```

In code, the same call:

```csharp
var authOptions = new TeslaAuthOptions
{
    ClientId = clientId,
    ClientSecret = clientSecret,
    Region = TeslaRegion.Europe,
};

using var authHttp = new HttpClient();
ITeslaAuthClient authClient = new TeslaAuthClient(authHttp, authOptions);

using var apiHttp = new HttpClient
{
    BaseAddress = TeslaEndpoints.For(TeslaRegion.Europe).FleetApiBaseUrl,
};
ITeslaPartnerClient partnerClient = new TeslaPartnerClient(apiHttp, authClient);

var account = await partnerClient.RegisterAsync("your-domain.example.com");
```

Two constraints worth knowing before you hit an opaque error:

- **The domain must be a bare host** — no scheme, no path. `coil.example.com`, not `https://coil.example.com/`.
- **The domain must match the root domain of an Allowed Origin** from step 1. For an allowed origin of `www.abc.com`, a registration domain of `123.abc.com` is accepted.

> **Registration is per region.** Registering against the EU host does *not* register you against the North America host. Calls into an unregistered region fail with region errors **even when the region is otherwise correct**, which is a genuinely misleading symptom. If your application serves both, run the command twice — once with `--region eu`, once with `--region na`.

Re-running the registration is safe and idempotent; it refreshes the key Tesla has on file, which is how you rotate keys.

## Step 5 — Verify what Tesla stored

```bash
coil partner public-key --domain <your-domain> --client-id <id> --client-secret <secret> --region eu
```

Tesla returns the registered key as a hex-encoded uncompressed EC point (`04…`), not as PEM. When a local keypair exists at `~/.coil/keys/`, the command compares the two and warns loudly on a mismatch — which is what you would see if the hosted file drifted from the key you actually sign with.

If this prints "no public key on file", the registration in step 4 did not take effect for this region.

## Step 6 — Pair a Virtual Key in the vehicle

The vehicle owner opens a deep link, `https://tesla.com/_ak/<your-domain>`, in the Tesla mobile app and confirms pairing. Only after this step can signed commands addressed to that vehicle succeed. A cheap check is `POST /api/1/vehicles/fleet_status` (`key_paired_vins` / `unpaired_vins`), see [API deviations](../api-deviations.md).

## Step 7 — Send signed commands

Once paired, `Coil.Commands` handles the session handshake (ECDH-derived shared secret), Protobuf envelope construction, HMAC-SHA256 tagging, and anti-replay counters behind a simple call shape, e.g. `await commands.SetChargeLimitAsync(vin, 80);` (with the 17-character VIN). `coil cmd <VIN> charge-limit 80` is a smoke-test wrapper around the same API. See [Signed Vehicle Commands](commands.md).

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| Registration fails mentioning the public key | The well-known URL is unreachable, redirects, requires auth, or serves the wrong content. Test it with `curl -fsS` from outside your network. |
| Registration fails mentioning the domain | The domain does not match the root domain of an Allowed Origin on developer.tesla.com. |
| Region errors on calls that look correctly routed | You registered in one region only. Register in the other region too (see step 4). |
| `coil partner public-key` warns about a mismatch | The hosted PEM is not the public half of the private key in `~/.coil/keys/`. Re-publish and re-register. |
| `coil login` fails with "redirect_uri ... not registered" | The redirect URI must be registered on developer.tesla.com exactly as used, and must use `localhost` (not `127.0.0.1`). Use `--port <n>` to get a stable URI such as `http://localhost:8080/callback`. |
| `coil partner public-key` answers `403 Unauthorized missing scopes` | The partner token needs more than `openid offline_access`; the CLI requests the right set, library users must set `TeslaAuthOptions.Scopes` (see [API deviations](../api-deviations.md)). |
| Signing fails later despite a successful registration | The key may not be P-256. Check with `openssl pkey -in private-key.pem -noout -text` — it must report `NIST CURVE: P-256`. |

## The `coil` CLI

`tools/Coil.Cli` (installed with `dotnet tool install --global Coil.Cli`; the command is `coil`) covers the flow above:

```bash
coil keys generate               # step 2 — keypair + ready-to-host PEM
coil keys show                   # print the public key (PEM + hex) for publishing
coil partner register            # step 4 — domain/public-key registration
coil partner public-key          # step 5 — verify what Tesla stored
coil login                       # OAuth flow via a local loopback redirect (http://localhost:<port>/callback)
coil vehicles                    # smoke test: list vehicles on the account
coil data <vin>                  # smoke test: read a vehicle data snapshot
coil wake <vin>                  # smoke test: wake a vehicle
coil cmd <vin> charge-limit 80   # smoke test: send a signed command
```

Check the CLI's own `--help` output for what's implemented in your installed version.
