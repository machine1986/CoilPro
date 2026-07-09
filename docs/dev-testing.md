# Lokales Live-Testing (M2 read endpoints)

Anleitung, um `coil login` → `coil vehicles` / `coil data <VIN>` gegen ein echtes Fahrzeug zu testen.
Stand: nach M2. Kein `coil`-Global-Tool installiert → Aufruf via `dotnet run`.

## 1. Client-ID + Secret bereitstellen (NIE committen)

`.env` (ist gitignored) im Repo-Root anlegen:

```
COIL_CLIENT_ID=dein-client-id
COIL_CLIENT_SECRET=dein-secret
```

Laden:

```bash
set -a && source .env && set +a
```

Alternativ direkt: `export COIL_CLIENT_ID=...` und ` export COIL_CLIENT_SECRET=...` (führendes Leerzeichen hält das Secret aus der History).

## 2. Redirect-URI in der Tesla-App hinterlegen

Auf developer.tesla.com in der App-Konfiguration unter **Allowed Redirect URIs** exakt eintragen (Tesla matcht exakt, fester Port nötig):

```
http://127.0.0.1:8080/callback
```

## 3. Login

```bash
dotnet run --project tools/Coil.Cli -c Release -- login --region eu --port 8080
```

Öffnet den Browser → Tesla-Login → Callback auf den Loopback. Tokens + Session (Client-ID + Region, **kein** Secret) werden nach `~/.coil/` geschrieben.

## 4. Daten lesen

```bash
dotnet run --project tools/Coil.Cli -c Release -- vehicles
dotnet run --project tools/Coil.Cli -c Release -- data <VIN>
dotnet run --project tools/Coil.Cli -c Release -- data <VIN> --endpoints charge_state,drive_state
```

## Bekannte Stolpersteine

- **Partner-Registrierung / Public Key:** Tesla verlangt i. d. R. eine einmalige Partner-Registrierung mit dem unter `https://<domain>/.well-known/appspecific/com.tesla.3p.public-key.pem` gehosteten Public Key (offener Vorbereitungspunkt #2, Domain = Subdomain von `dotnetservices.de`). Ohne das kann der Zugriff fehlschlagen. Tooling dafür (`coil keys generate` / `coil partner register`) kommt in M3.
- **Zu verifizieren gegen echte Responses** (in [`api-deviations.md`](./api-deviations.md) eintragen): Rate-Limit-Header-Namen (`x-ratelimit-*`), Charging-History-Pfad/Shape (`api/1/dx/charging/history`), Error-Body-Shape (`{ error, error_description }`), Endpunkt-Hosts.

## Optional: `coil` global installieren

```bash
dotnet pack tools/Coil.Cli -c Release -o ./artifacts
dotnet tool install --global --add-source ./artifacts Coil.Cli
# danach nur noch: coil login --region eu --port 8080
```
