# HVV-API – Recherche-Notizen

> Dokumentation der API-Recherche für das Abfahrtsmonitor-Feature (Phase 4.6 der Roadmap).
> Stand: Mai 2026
> **Zweck:** Festhalten, *wie* und *warum* die Implementierung den inoffiziellen
> `geofox/departureList`-Endpoint nutzt, damit das Future-Self diese Entscheidungen
> nicht nochmal von Null herleiten muss.

---

## Zusammenfassung

Der HVV bietet drei mögliche Wege, an Abfahrtsdaten zu kommen:

1. **Offizieller Abfahrtsmonitor-Link** als iFrame einbetten – regelkonform, aber kein Lerneffekt und optisch nicht ins Dashboard integrierbar.
2. **Inoffizieller `geofox/departureList`-Endpoint** der HVV-Webseite – technisch sauber nutzbar, rechtliche Grauzone.
3. **Offizielle Geofox-API (GTI)** mit Credentials per E-Mail beantragen – setzt voraus, dass eine *öffentlich zugängliche, kostenlose Fahrplanauskunft für hvv-Fahrgäste* betrieben wird. Trifft auf ein privates Dashboard nicht zu.

**Gewählt: Variante 2.** Begründung in [Rechtliche Einordnung](#rechtliche-einordnung).

---

## Architektur des Webseiten-Workflows

Die HVV-Abfahrtsanzeige unter `www.hvv.de/de/fahrplaene/.../abfahrten-anzeige?show={UUID}` macht im Browser zwei Calls:

```
1. GET  https://www.hvv.de/linking-service/abfahrten/show/{UUID}
        → Konfiguration (welche Haltestelle, welche Linien-Filter)

2. POST https://www.hvv.de/geofox/departureList
        → Abfahrtsdaten (alle paar Sekunden gepollt)
```

Die UUID im `show`-Parameter ist nur ein Kürzel für die Konfiguration auf dem HVV-Server. **Für das eigene Projekt ist der erste Call irrelevant** – die Konfiguration kommt aus der eigenen `appsettings.json`, der zweite Call wird direkt nachgebaut.

Wort `geofox` in der URL: Das ist die offizielle interne Geofox-API (Geofox Thin Interface, GTI). Die hvv.de-Webseite proxt darauf ohne Authentifizierung nach außen, daher die freie Erreichbarkeit.

---

## Endpoint-Details

### Request

- **Methode:** `POST`
- **URL:** `https://www.hvv.de/geofox/departureList`
- **Pflicht-Header:** `Content-Type: application/json`, `Accept: application/json`
- **Authentifizierung:** keine
- **Cookies:** nicht erforderlich (Test ohne Cookies erfolgreich)
- **Origin/Referer:** nicht erforderlich (Test ohne diese Header erfolgreich)

### Request-Body (Beispiel)

```json
{
  "version": 47,
  "stations": [
    {
      "name": "Wedel, Feldstraße",
      "id": "Master:85001",
      "city": "Wedel",
      "type": "STATION"
    }
  ],
  "filter": [
    { "serviceID": "VHH:189_VHH", "stationIDs": ["Master:81001"] },
    { "serviceID": "VHH:189_VHH", "stationIDs": ["Master:85002"] }
  ],
  "time": { "date": "04.05.2026", "time": "08:00" },
  "maxList": 20,
  "maxTimeOffset": 120,
  "useRealtime": true,
  "allStationsInChangingNode": true
}
```

### Feldsemantik im Request

| Feld | Bedeutung |
|------|-----------|
| `version` | API-Version. Aktuell `47`. Hardcoden, in Konstante festhalten. Bei `version`-Bump bricht meistens etwas. |
| `stations[].id` | HVV-interne Stations-ID im Format `Master:xxxxx`. Wird via Webseiten-Generator ermittelt. |
| `stations[].type` | `STATION` (sonst auch `ADDRESS`, `POI` möglich – für unseren Use-Case nur Stationen relevant). |
| `filter` | Linien-Filter. Pro Eintrag: Linie × erlaubte nächste Haltestelle. So filtert HVV Richtungen. |
| `time` | Pflichtfeld. Format `dd.MM.yyyy` und `HH:mm`. Server-Zeit zum Anfragezeitpunkt. |
| `maxList` | Maximale Anzahl Departures in der Response. |
| `maxTimeOffset` | Zeitfenster in die Zukunft (Minuten). |
| `useRealtime` | Echtzeitdaten aktivieren. Immer `true` setzen. |
| `allStationsInChangingNode` | Bei Knotenpunkten (z.B. Bahnhöfen) alle Bussteige zusammenfassen. |

### Response (Beispiel, gekürzt)

```json
{
  "returnCode": "OK",
  "time": { "date": "04.05.2026", "time": "08:00" },
  "departures": [
    {
      "line": {
        "name": "189",
        "direction": "S Blankenese",
        "origin": "S Wedel",
        "type": {
          "simpleType": "BUS",
          "shortInfo": "Bus",
          "longInfo": "Niederflur Stadtbus",
          "model": "Niederflur Stadtbus"
        },
        "id": "VHH:189_VHH",
        "dlid": "de:hvv:189:"
      },
      "directionId": 1,
      "timeOffset": 6,
      "delay": 0,
      "serviceId": 41060,
      "station": {
        "combinedName": "Wedel, Feldstraße",
        "id": "Master:85001",
        "globalId": "de:01056:85001"
      },
      "attributes": [
        {
          "isPlanned": false,
          "value": "Unbekannte Ursache",
          "types": ["REALTIME", "UNRECOGNIZED"]
        }
      ]
    }
  ]
}
```

### Feldsemantik in der Response

| Feld | Bedeutung |
|------|-----------|
| `returnCode` | `"OK"` bei Erfolg, `"ERROR_TEXT"` bei Validierungsfehler (mit `errorText`-Feld). |
| `time` | Server-Zeit, **gegen die `timeOffset` zu rechnen ist**. |
| `departures[].timeOffset` | Minuten ab Server-`time` bis zur Abfahrt. → In der Domain in `DateTimeOffset` umrechnen. |
| `departures[].delay` | **Sekunden** Verspätung. **Nullable.** Fehlt komplett, wenn keine Echtzeitdaten verfügbar. |
| `departures[].attributes` | Optional. Enthält Marker wie `REALTIME` (= Echtzeitdaten vorhanden), Hinweistexte, Störungsgründe. |
| `line.type.simpleType` | `BUS`, `STRAIN` (S-Bahn), `UTRAIN` (U-Bahn), `FERRY`, etc. → In Domain-Enum mappen. |
| `line.type.shortInfo` | UI-tauglicher Kurztext, z.B. `"Bus"` vs. `"Nachtbus"`. |
| `directionId` | Numerische Richtungs-ID. Für Sortierung/Filterung intern interessant, in UI eher irrelevant. |

---

## Wichtige semantische Fallen

### `delay: null` ≠ `delay: 0`

Diese Unterscheidung ist nicht kosmetisch, sondern bedeutungstragend:

- `delay: 0` → Echtzeitdaten verfügbar, Bus ist **pünktlich**.
- `delay` fehlt komplett → **keine Echtzeitdaten** für diese Abfahrt verfügbar (i.d.R. weil zu weit in der Zukunft).

In der Domain als zwei verschiedene Zustände modellieren. Niemals `?? 0` schreiben – das vernichtet die Semantik. Stattdessen:

```csharp
public sealed record Departure(/* ... */ TimeSpan? Delay)
{
    public bool HasLiveData => Delay.HasValue;
}
```

### `timeOffset` ist relativ zur Server-Zeit, nicht zur Client-Zeit

Beim Mapping unbedingt mit der `time` aus der Response rechnen, **nicht** mit `DateTime.Now`. Der Client kann andere Uhrzeit haben, andere Zeitzone, andere Sommer-/Winterzeit-Auffassung.

```csharp
var serverTime = ParseHvvTime(response.Time);  // → DateTimeOffset CET
var planned = serverTime.AddMinutes(dto.TimeOffset);
```

### Zeitzone

HVV-Server liefert immer Berlin-Zeit (CET/CEST). Beim Parsen `TimeZoneInfo` für `Europe/Berlin` explizit verwenden, damit das auch im UTC-Container auf dem Pi korrekt ist.

### Pflichtfeld `time` im Request

Fehlt das `time`-Feld, gibt es kryptische Fehler. Immer aktuelle Zeit mitschicken, idealerweise vom Server selbst (NTP-synchron).

---

## Stations- und Linien-IDs ermitteln

Es gibt im eigenen Code keinen Lookup-Mechanismus für Stations-IDs (das wäre ein zweiter API-Endpoint, den wir nicht erschließen). Workflow stattdessen:

1. Auf <https://abfahrten.hvv.de> die gewünschte Haltestelle und Linien konfigurieren.
2. Generierten Link öffnen, in Browser-DevTools den ersten Call (`linking-service/abfahrten/show/{UUID}`) anschauen.
3. Aus der Response (`stationList`/`filterList`) die IDs in die eigene `appsettings.json` übernehmen.

Das ist ein einmaliger manueller Schritt pro Haltestelle. Für ein Kiosk-Dashboard mit 1–3 festen Haltestellen vollkommen ausreichend.

---

## Konfigurations-Skizze (`appsettings.json`)

```json
"Hvv": {
  "Endpoint": "https://www.hvv.de/geofox/departureList",
  "PollIntervalSeconds": 60,
  "Stations": [
    {
      "Name": "Wedel, Feldstraße",
      "MasterId": "Master:85001",
      "City": "Wedel",
      "Filters": [
        { "ServiceId": "VHH:189_VHH", "TargetStationId": "Master:81001" },
        { "ServiceId": "VHH:189_VHH", "TargetStationId": "Master:85002" }
      ],
      "MaxList": 20,
      "MaxTimeOffsetMinutes": 120
    }
  ]
}
```

Pro Haltestelle ein eigener Eintrag, mehrere Haltestellen prinzipiell unterstützbar (Tile zeigt dann z.B. nur die nächste, oder kann durchgewechselt werden – UI-Entscheidung).

---

## Architektur-Empfehlungen für die Implementierung

Wiederverwendung der Patterns aus Phase 4.4 (Wetter) und 4.5 (Fußball):

1. **Typed `HttpClient`** via `IHttpClientFactory` (`HvvDepartureClient`).
2. **DTOs** in `Dashboard.Infrastructure/Hvv/Dtos/` – 1:1 zum Wire-Format. Reine Datenträger.
3. **Mapping-Schicht** DTO → Domain. Hier passiert: `timeOffset` → `DateTimeOffset`, `delay` (Sekunden) → `TimeSpan?`, `simpleType` → Domain-Enum.
4. **Domain-Modell** in `Dashboard.Domain` – API-frei, stabil, testbar.
5. **`BackgroundService`** pollt im konfigurierten Intervall, legt Ergebnis in `IMemoryCache`.
6. **Polly** drumherum: Retry mit Exponential Backoff, Circuit Breaker bei wiederholten Fehlern, Timeout pro Request.
7. **Sample-JSON-Files** in `Dashboard.Tests/TestData/` – sowohl Erfolgs- als auch Fehler-Response, sowohl mit als auch ohne Echtzeitdaten. Damit Mapping-Tests offline und deterministisch laufen.

### Mapping-Skizze

```csharp
private static Departure Map(DepartureDto dto, DateTimeOffset serverTime)
{
    var planned = serverTime.AddMinutes(dto.TimeOffset);
    var delay = dto.Delay.HasValue
        ? TimeSpan.FromSeconds(dto.Delay.Value)
        : (TimeSpan?)null;

    return new Departure(
        LineName: dto.Line.Name,
        Direction: dto.Line.Direction,
        Mode: MapMode(dto.Line.Type.SimpleType),
        ShortInfo: dto.Line.Type.ShortInfo,
        PlannedTime: planned,
        Delay: delay);
}
```

---

## Browser-Headers: Brauchen wir sie?

**Test-Ergebnis:** Nein. Der Endpoint funktioniert mit minimalen Headers (`Content-Type`, `Accept`). Kein `Origin`, kein `Referer`, kein `X-Platform`, kein Cookie nötig.

**Empfehlung trotzdem:** Realistischen `User-Agent` mitschicken, damit Logs auf HVV-Seite einen Hinweis auf die Quelle zeigen. Z.B.:

```
User-Agent: ipad-kiosk-dashboard/1.0 (+https://github.com/{username}/{repo})
```

Das ist gute Netiquette für Self-Hoster und macht es dem HVV einfacher, im Bedarfsfall freundlich Kontakt aufzunehmen, statt einfach den Endpoint dichtzumachen.

---

## Rechtliche Einordnung

**Status: Inoffizielle Nutzung eines öffentlich erreichbaren Endpoints.**

Der HVV bietet zwei offizielle Wege an:

1. **Abfahrtsmonitor-Link** mit eigenen Nutzungsbedingungen, die u.a. fordern: *„Die abrufbaren Informationen erscheinen unverändert und eigenständig"* und *„Der Quellcode des ausgelieferten Links wird durch den Verwender nicht verändert."* → Verträgt sich nicht mit eigener UI, eigenem Parsing.
2. **Geofox-API mit Credentials** – setzt voraus, dass eine kostenlose öffentliche Fahrplanauskunft für hvv-Fahrgäste betrieben wird. Trifft auf ein privates Dashboard nicht zu.

Die hier gewählte Variante (direkter Aufruf des `geofox/departureList`-Endpoints ohne Credentials) ist **vom HVV nicht explizit erlaubt** und wäre formal ein Verstoß gegen die Abfahrtsmonitor-Nutzungsbedingungen. Praktische Realität:

- Endpoint ist öffentlich erreichbar, ohne Auth, ohne Rate-Limiting nach außen.
- Nutzung erfolgt für Privatzwecke, ein Gerät, geringe Frequenz (1 Request/Minute).
- Vergleichbare Open-Source-Projekte existieren seit Jahren ohne erkennbare Konsequenzen.
- HVV ist eine öffentlich-rechtlich getragene Verkehrsgesellschaft, keine klagewütige Organisation.

**Maßnahmen zur Risikominimierung:**

- **Polling-Frequenz konservativ:** maximal 1 Request pro Minute pro Haltestelle.
- **Aggressives Caching:** Mehrere UI-Abfragen innerhalb eines Polling-Zyklus werden aus dem Cache bedient.
- **Identifizierender User-Agent:** Höfliche Kontaktaufnahme im Fall der Fälle ermöglichen.
- **Ehrliche README-Dokumentation:** Hinweis, dass dies ein inoffizieller Endpoint ist und für ernsthafte Projekte der offizielle Geofox-Zugang der korrekte Weg wäre.
- **Graceful Degradation:** Sollte der Endpoint plötzlich nicht mehr erreichbar sein, zeigt die UI einen freundlichen „Daten gerade nicht verfügbar"-Zustand statt einen Fehler.

**Disclaimer:** Diese Einschätzung ist keine Rechtsberatung. Letzte Verantwortung liegt beim Repo-Eigentümer. Im Zweifel offiziellen Geofox-Zugang beantragen oder den Abfahrtsmonitor-Link per iFrame einbetten.

---

## README-Hinweis (Vorlage für später)

```markdown
### HVV-Abfahrtsmonitor

Diese Komponente nutzt einen inoffiziellen JSON-Endpoint der HVV-Webseite
(`www.hvv.de/geofox/departureList`), der keine Authentifizierung erfordert.
Für eigene, ernsthafte Projekte wird empfohlen, den
[offiziellen Geofox-API-Zugang](https://www.hvv.de/de/fahrplaene/abruf-fahrplaninfos/datenabruf)
beim HVV per E-Mail zu beantragen.

Die Polling-Frequenz ist bewusst konservativ (1 Request pro Minute pro Haltestelle),
um die Last auf den HVV-Servern minimal zu halten.
```

---

## Offene Fragen / spätere Verifikation

- [ ] Verhalten bei echten Verspätungen testen: Ist `delay` wirklich in Sekunden, oder Minuten? (Bei Test war überall `delay: 0`. Bei echter Verspätung im Feld nochmal prüfen.)
- [ ] Verhalten bei Ausfall (`Cancelled` o.ä.): Welches Attribut markiert ausgefallene Fahrten? (Vermutung: in `attributes[]` mit speziellem `type`.)
- [ ] Maximale `maxList`-Größe – gibt es eine harte Obergrenze?
- [ ] Verhalten bei mehreren Stationen im `stations`-Array – mischt der Server die Departures, oder gibt es separate Arrays?

Diese Punkte beim Implementieren des Features in 4.6 nachverifizieren und in diese Datei einpflegen.