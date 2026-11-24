# Lokales Live-Testing (M2 read endpoints)

Anleitung, um `coilpro login` → `coilpro vehicles` / `coilpro data <VIN>` gegen ein echtes Fahrzeug zu testen.
Stand: nach M2 + Partner-Onboarding. Kein `coilpro`-Global-Tool installiert → Aufruf via `dotnet run`.

## 0. Partner-Registrierung (einmalig, VOR dem ersten Login)

Tesla verlangt eine einmalige Partner-Registrierung mit einem öffentlich gehosteten Public Key,
bevor die Fleet API für die eigene Anwendung nutzbar ist. Ohne diesen Schritt schlagen selbst
Read-Calls fehl.

```bash
dotnet run --project tools/CoilPro.Cli -c Release -- keys generate
# public-key.pem so veröffentlichen, dass diese URL sie ausliefert:
#   https://<domain>/.well-known/appspecific/com.tesla.3p.public-key.pem
curl -fsS https://<domain>/.well-known/appspecific/com.tesla.3p.public-key.pem   # prüfen!

dotnet run --project tools/CoilPro.Cli -c Release -- partner register   --domain <domain> --region eu
dotnet run --project tools/CoilPro.Cli -c Release -- partner public-key --domain <domain> --region eu
```

Die Domain muss zur Root-Domain einer **Allowed Origin** auf developer.tesla.com passen.
Details und Fehlerbilder: [`articles/partner-onboarding.md`](./articles/partner-onboarding.md).

> **Pro Region registrieren.** Eine Registrierung gegen den EU-Host gilt *nicht* für den NA-Host —
> Calls in eine nicht registrierte Region scheitern mit Region-Fehlern, obwohl die Region stimmt.

## 1. Client-ID + Secret bereitstellen (NIE committen)

`.env` (ist gitignored) im Repo-Root anlegen:

```
COILPRO_CLIENT_ID=dein-client-id
COILPRO_CLIENT_SECRET=dein-secret
```

Laden:

```bash
set -a && source .env && set +a
```

Alternativ direkt: `export COILPRO_CLIENT_ID=...` und ` export COILPRO_CLIENT_SECRET=...` (führendes Leerzeichen hält das Secret aus der History).

## 2. Redirect-URI in der Tesla-App hinterlegen

Auf developer.tesla.com in der App-Konfiguration unter **Allowed Redirect URIs** exakt eintragen (Tesla matcht exakt, fester Port nötig):

```
http://localhost:8080/callback
```

`localhost`, nicht `127.0.0.1` — developer.tesla.com akzeptiert nur `localhost` (Stand 2026-11-24). Die CLI lauscht deshalb auf `localhost`.

## 3. Login

```bash
dotnet run --project tools/CoilPro.Cli -c Release -- login --region eu --port 8080
```

Öffnet den Browser → Tesla-Login → Callback auf den Loopback. Tokens + Session (Client-ID + Region, **kein** Secret) werden nach `~/.coilpro/` geschrieben.

## 4. Daten lesen

```bash
dotnet run --project tools/CoilPro.Cli -c Release -- vehicles
dotnet run --project tools/CoilPro.Cli -c Release -- data <VIN>
dotnet run --project tools/CoilPro.Cli -c Release -- data <VIN> --endpoints charge_state,drive_state
dotnet run --project tools/CoilPro.Cli -c Release -- wake <VIN>
```

## Signed commands (M3)

Voraussetzungen: Schritt 0 (Partner-Registrierung, Public Key gehostet), Login mit den Scopes `vehicle_cmds` und `vehicle_charging_cmds` (in den Standard-Scopes von `coilpro login` enthalten — ein vor M3 erzeugter Login muss ggf. wiederholt werden) und der Private Key unter `~/.coilpro/keys/private-key.pem` (`coilpro keys generate`).

**1. Virtual Key installieren (einmalig).** Auf dem Handy mit Tesla-App `https://tesla.com/_ak/<domain>` öffnen, die Anfrage bestätigen und im Auto mit Key-Card/Handy-Key freigeben. Ohne diesen Schritt endet jedes Kommando mit Exit-Code 3 (`TeslaKeyNotPairedException`).

**2. Harmloser Erstest: Lichter blinken.**

```bash
dotnet run --project tools/CoilPro.Cli -c Release -- cmd <VIN> flash
```

Erwartet: `OK` (Exit 0), das Auto blinkt. Schläft das Fahrzeug: Exit 4 → `coilpro wake <VIN>` oder `--wake` (kostenpflichtiger Wake-Call).

**3. Ladelimit setzen** (reversibel; vorher mit `coilpro data <VIN>` den aktuellen Wert notieren):

```bash
dotnet run --project tools/CoilPro.Cli -c Release -- cmd <VIN> charge-limit 80
dotnet run --project tools/CoilPro.Cli -c Release -- cmd <VIN> charge-limit 80   # zweiter Aufruf: "Refused: already_set" (Exit 2) ist korrekt
```

**4. Verriegeln / Entriegeln** (nur in unmittelbarer Nähe des Autos testen):

```bash
dotnet run --project tools/CoilPro.Cli -c Release -- cmd <VIN> lock
dotnet run --project tools/CoilPro.Cli -c Release -- cmd <VIN> unlock
```

Exit-Codes: `0` OK, `1` Fehler, `2` vom Fahrzeug verweigert (`Refused: <Grund>`), `3` Key nicht gepairt, `4` Fahrzeug schläft. `<VIN>` ist die 17-stellige VIN, nicht die numerische ID. Ergebnisse und Abweichungen (Fault-Codes, Fehlerbilder) in [`api-deviations.md`](./api-deviations.md) nachtragen.

## Bekannte Stolpersteine

- **Ohne Schritt 0 geht nichts.** Die Partner-Registrierung ist keine Formalie für später — sie ist Voraussetzung für den API-Zugriff.
- **`403 Forbidden` auf die well-known-URL, obwohl die Datei hochgeladen ist** → Dateirechte. `sftp put` überträgt die lokalen Rechte; Key-Dateien aus `coilpro keys generate` vor dem Fix vom 2026-11-24 waren `0600`, Apache darf sie dann nicht lesen. Lösung: in `sftp` `chmod 644 <pfad>/com.tesla.3p.public-key.pem`. Diagnose-Trick: Eine nicht existierende Datei im selben Ordner liefert `404` — dann ist es definitiv ein Rechte-Problem, kein Pfad-Problem. Neu erzeugte Public Keys schreibt die CLI inzwischen mit `0644` (der Private Key bleibt `0600`).
- **Subdomain bei united-domains:** im Portal *Administration (DNS) → Subdomains* anlegen und danach unter *Webspace → Domain verknüpfen* mit einem Ordner verbinden. Ohne Verknüpfung zeigt die Subdomain auf die Standard-Weiterleitung (`167.86.96.87`), die auf 443 gar nicht antwortet. Das SSL-Zertifikat stellt united-domains nach dem Verknüpfen automatisch aus (Minuten).
- **Live verifiziert am 2026-11-24 (EU, Model Y)** — Details in [`api-deviations.md`](./api-deviations.md): Rate-Limit-Header (`ratelimit-<bucket>-*`, nicht `x-ratelimit-*`), Charging-History (Pfad stimmt, aber ohne `response`-Envelope), `endpoints`-Trennzeichen `;` (Komma liefert stillschweigend nichts), Standort nur mit `location_data`.
- **Login-Fallstricke, alle behoben:** Authorize läuft über `auth.tesla.com` (nicht `fleet-auth`); Redirect-URI muss `localhost` sein; der Browser-Link muss escaped übergeben werden (sonst „redirect_uri not registered“).

## Optional: `coilpro` global installieren

```bash
dotnet pack tools/CoilPro.Cli -c Release -o ./artifacts
dotnet tool install --global --add-source ./artifacts CoilPro.Cli
# danach nur noch: coilpro login --region eu --port 8080
```
