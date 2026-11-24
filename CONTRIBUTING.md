# Contributing to CoilPro

Thanks for your interest. CoilPro is an unofficial SDK for the Tesla Fleet API — not affiliated with Tesla, Inc.

## Build and test

You need the .NET 10 SDK and the .NET 8 SDK (libraries multi-target `net8.0;net10.0`).

```bash
dotnet restore
dotnet build
dotnet test
```

Documentation site (API metadata + articles):

```bash
dotnet tool restore
dotnet docfx docs/docfx.json   # output: docs/_site
```

## Conventions

- Libraries target `net8.0;net10.0`; CLI, tests and samples `net10.0`. `Nullable` and `ImplicitUsings` are on, `LangVersion` is `latest` (see `Directory.Build.props`).
- Cryptography only through `System.Security.Cryptography` — no BouncyCastle unless unavoidable.
- Models are `record` types, JSON via `System.Text.Json`, deserialization tolerant of unknown fields.
- HTTP through `IHttpClientFactory`, resilience via Polly.
- Prefer simple, direct solutions over heavy patterns.
- Naming: product and packages use `CoilPro` (`CoilPro.Commands`, `CoilPro.AspNetCore`, `CoilPro.Cli`); types describing Tesla's API keep the `Tesla` prefix (`ITeslaAuthClient`, `TeslaApiException`).
- Public APIs need XML documentation.
- **Verify Tesla API details before implementing them** against [developer.tesla.com](https://developer.tesla.com) and [teslamotors/vehicle-command](https://github.com/teslamotors/vehicle-command), and record differences in [`docs/api-deviations.md`](./docs/api-deviations.md).
- Never use "Tesla" as a leading brand name in new product or package names.

## Live tests with your own credentials

The automated tests do not call Tesla. To try the SDK against a real account and vehicle you need your own
developer application, domain and registered key (see [Partner Onboarding](./docs/articles/partner-onboarding.md)).
Provide credentials through environment variables (`COILPRO_CLIENT_ID`, `COILPRO_CLIENT_SECRET`, optionally
`COILPRO_REGION`, `COILPRO_DOMAIN`) or a gitignored `.env` file that you load into your shell (`set -a && source .env && set +a`), then use the CLI:

```bash
dotnet run --project tools/CoilPro.Cli -- login --region eu --port 8080
dotnet run --project tools/CoilPro.Cli -- vehicles
```

Live calls can cost money (the Fleet API is pay-per-use) and signed commands act on a real vehicle — start
with harmless ones such as `flash`.

## Never commit secrets

Do not commit client secrets, tokens, private keys (`private-key.pem`, anything under `~/.coilpro/`), `.env`
files, or VINs/locations from your own vehicle. Redact them in issues, logs and test fixtures.

## Pull requests

Keep changes focused, add or update tests, make sure `dotnet build` and `dotnet test` pass, and describe what
you verified (and against which region/vehicle, if live).
