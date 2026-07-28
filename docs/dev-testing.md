# Lokales Live-Testing (M2 read endpoints)

Anleitung, um `coil login` → `coil vehicles` / `coil data <VIN>` gegen ein echtes Fahrzeug zu testen.
Stand: nach M2 + Partner-Onboarding. Kein `coil`-Global-Tool installiert → Aufruf via `dotnet run`.

## 0. Partner-Registrierung (einmalig, VOR dem ersten Login)

Tesla verlangt eine einmalige Partner-Registrierung mit einem öffentlich gehosteten Public Key,
bevor die Fleet API für die eigene Anwendung nutzbar ist. Ohne diesen Schritt schlagen selbst
Read-Calls fehl.

```bash
dotnet run --project tools/Coil.Cli -c Release -- keys generate
# public-key.pem so veröffentlichen, dass diese URL sie ausliefert:
#   https://<domain>/.well-known/appspecific/com.tesla.3p.public-key.pem
curl -fsS https://<domain>/.well-known/appspecific/com.tesla.3p.public-key.pem   # prüfen!

dotnet run --project tools/Coil.Cli -c Release -- partner register   --domain <domain> --region eu
dotnet run --project tools/Coil.Cli -c Release -- partner public-key --domain <domain> --region eu
```

Die Domain muss zur Root-Domain einer **Allowed Origin** auf developer.tesla.com passen.
Details und Fehlerbilder: [`articles/partner-onboarding.md`](./articles/partner-onboarding.md).

> **Pro Region registrieren.** Eine Registrierung gegen den EU-Host gilt *nicht* für den NA-Host —
> Calls in eine nicht registrierte Region scheitern mit Region-Fehlern, obwohl die Region stimmt.

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
dotnet run --project tools/Coil.Cli -c Release -- wake <VIN>
```

## Bekannte Stolpersteine

- **Ohne Schritt 0 geht nichts.** Die Partner-Registrierung ist keine Formalie für später — sie ist Voraussetzung für den API-Zugriff.
- **Zu verifizieren gegen echte Responses** (in [`api-deviations.md`](./api-deviations.md) eintragen): Rate-Limit-Header-Namen (`x-ratelimit-*`), Charging-History-Pfad/Shape (`api/1/dx/charging/history`), Endpunkt-Hosts, `vehicle_data`-`endpoints`-Trennzeichen (`;`). Der Error-Body-Shape ist inzwischen bestätigt.

## Optional: `coil` global installieren

```bash
dotnet pack tools/Coil.Cli -c Release -o ./artifacts
dotnet tool install --global --add-source ./artifacts Coil.Cli
# danach nur noch: coil login --region eu --port 8080
```
