# Partner Onboarding

This is the part of integrating with the Tesla Fleet API that trips up almost every third-party developer: registering an application, hosting a public key, and pairing a Virtual Key in the vehicle before a single signed command can be sent. Coil.NET's goal is to make this a guided, well-documented process instead of a scavenger hunt through forum threads.

This article is an **outline** — the anchor page this topic deserves. Concrete step-by-step instructions, screenshots, and CLI walkthroughs will be filled in as the underlying milestones ship (noted per step below). All endpoint paths and requirements below must be re-verified against [developer.tesla.com](https://developer.tesla.com) at implementation time — Tesla changes these without much notice — and any confirmed deviation gets recorded in [`docs/api-deviations.md`](../api-deviations.md).

## Overview

| # | Step | Needed for | Status |
|---|---|---|---|
| 1 | Register a Tesla developer application | OAuth login, partner token | Available now (M1) |
| 2 | Obtain a partner (application) token | Registering your domain/public key | Available now (M1) — `GetPartnerTokenAsync` |
| 3 | Generate an EC P-256 keypair | Signing vehicle commands | Prep now, tooling in M3 |
| 4 | Host the public key at `/.well-known/appspecific/com.tesla.3p.public-key.pem` | Domain verification | Prep now, tooling in M3 |
| 5 | Register your domain (public key) with Tesla | Domain verification | M3 |
| 6 | Pair a Virtual Key in the vehicle (deep link) | Sending signed commands | M3 |
| 7 | Send signed commands (`Coil.Commands`) | Vehicle Command Protocol | M3 |

## Step 1 — Register a Tesla developer application

Create an application at the [Tesla developer dashboard](https://developer.tesla.com/en_US/dashboard). This gives you a Client ID (and, for confidential clients, a Client Secret) and lets you configure a redirect URI — everything `TeslaAuthOptions` needs for the authorization-code flow described in [Getting Started](getting-started.md).

## Step 2 — Obtain a partner (application) token

Domain/public-key registration (step 5) is called with a partner token, not a user token. Coil.NET exposes this as the client-credentials flow:

```csharp
var partnerTokens = await authClient.GetPartnerTokenAsync();
```

## Step 3 — Generate an EC P-256 keypair (prep now, tooling in M3)

The Vehicle Command Protocol signs every command with an EC P-256 (NIST) keypair. The private key never leaves your infrastructure; only the public key is published and registered. Key generation itself needs no vehicle and no signed-command support, so it's reasonable to prepare this ahead of M3 — e.g. with `openssl ecparam -name prime256v1 -genkey -noout` — but Coil.NET's own tooling for this (`coil keys generate`, producing a ready-to-host PEM) ships with the `Coil.Commands` milestone (M3).

## Step 4 — Host the public key (prep now, tooling in M3)

The public key must be reachable, unauthenticated, over HTTPS at:

```
https://<your-domain>/.well-known/appspecific/com.tesla.3p.public-key.pem
```

This is plain static file hosting — no Coil.NET dependency — so the domain and hosting can be set up ahead of M3. Keep the file path and content type (`application/x-pem-file` or `text/plain`) stable; Tesla's registration step (below) verifies it lives at exactly this well-known path.

## Step 5 — Register your domain with Tesla (M3)

Using the partner token from step 2, register the domain that hosts your public key with Tesla's partner accounts endpoint (`POST /api/1/partner_accounts` at the time of writing — **verify against developer.tesla.com before relying on this**). The planned `coil partner register` CLI command wraps this call.

## Step 6 — Pair a Virtual Key in the vehicle (M3)

The vehicle owner opens a deep link, `https://tesla.com/_ak/<your-domain>`, in the Tesla mobile app and confirms pairing. Only after this step can signed commands addressed to that vehicle succeed.

## Step 7 — Send signed commands (M3)

Once paired, `Coil.Commands` handles the session handshake (ECDH-derived shared secret), Protobuf envelope construction, HMAC-SHA256 tagging, and anti-replay counters behind a simple call shape, e.g. `await client.Commands.SetChargeLimit(vin, 80);`. The planned `coil cmd <vin> charge-limit 80` CLI command is a smoke-test wrapper around the same API.

## The `coil` CLI (planned command surface)

`tools/Coil.Cli` (installable as `dotnet tool install -g coil`) is intended to lower the friction of the steps above:

```bash
coil keys generate      # step 3 — keypair + ready-to-host PEM
coil partner register   # step 5 — domain/public-key registration
coil login              # OAuth flow via a local loopback redirect
coil vehicles           # smoke test: list vehicles on the account
coil wake <vin>         # smoke test: wake a vehicle
coil cmd <vin> charge-limit 80   # smoke test: send a signed command (M3)
```

These commands are the target surface from the technical specification; check the CLI's own `--help` output for what's actually implemented in your installed version.
