# Funktionale Anforderungen – iPad-Kiosk-Dashboard
> MoSCoW-Priorisierung | Stand: Mai 2026

---

## 1 · Uhrzeit & Datum

| ID | Anforderung | Prio |
|----|-------------|------|
| FA-1.01 | Das Dashboard zeigt die aktuelle Uhrzeit an, die sich in Echtzeit aktualisiert. | M |
| FA-1.02 | Das Dashboard zeigt das aktuelle Datum mit Wochentag an. | M |

---

## 2 · Wetter

| ID | Anforderung | Prio |
|----|-------------|------|
| FA-2.01 | Das Dashboard zeigt das aktuelle Wetter für einen konfigurierten Standort an (Temperatur, Wetterlage). | M |
| FA-2.02 | Das Dashboard zeigt eine Wettervorhersage für den morgigen Tag an. | M |
| FA-2.03 | Wetterdaten werden automatisch regelmäßig aktualisiert (Push, kein manueller Reload). | M |
| FA-2.04 | Das Dashboard zeigt die Regenwahrscheinlichkeit für heute und morgen an. | S |
| FA-2.05 | Das Dashboard zeigt eine stündliche Vorschau für die nächsten 3–4 Stunden an (Temperatur + Niederschlag). | S |

---

## 3 · Habit-Tracker

| ID | Anforderung | Prio |
|----|-------------|------|
| FA-3.01 | Es existieren genau 5 vordefinierte Habits: Gym, Zone-2-Lauf, VO2-Max-Intervalle, Seilspringen, Dehnen. | M |
| FA-3.02 | Jeder Habit kann für den heutigen Tag per Touch als erledigt markiert werden. | M |
| FA-3.03 | Ein bereits markierter Habit kann für den heutigen Tag wieder zurückgenommen werden. | M |
| FA-3.04 | Das Dashboard zeigt an, wie oft jeder Habit in der aktuellen Woche (Mo–So) erledigt wurde. | M |
| FA-3.05 | Das Dashboard zeigt an, wie oft jeder Habit im aktuellen Kalenderjahr erledigt wurde. | M |
| FA-3.06 | Habits können beliebig rückwirkend für vergangene Tage eingetragen werden. | S |
| FA-3.07 | Bei Lauf-Habits (Zone-2, VO2-Max) kann optional eine Dauer (Minuten) und eine Pace/Geschwindigkeit eingetragen werden. | C |
| FA-3.08 | Habit-Daten werden als Verlaufsgraph visualisiert (z.B. Aktivität pro Woche als Balkendiagramm). | C |
| FA-3.09 | Der Habit-Bereich enthält ein visuelles Element, über das die Lauf-Heatmap-Ansicht aufgerufen werden kann (Verlinkung zu Abschnitt 8). | M |

---

## 4 · Fußball

| ID | Anforderung | Prio |
|----|-------------|------|
| FA-4.01 | Das Dashboard zeigt die letzten Spielergebnisse für Real Madrid und HSV an. | M |
| FA-4.02 | Das Dashboard zeigt die nächsten bevorstehenden Spiele dieser zwei Vereine an. | M |
| FA-4.03 | Das Dashboard zeigt die aktuelle Tabellenposition dieser zwei Vereine in ihrer jeweiligen Liga an. | M |
| FA-4.04 | Fußballdaten werden automatisch regelmäßig aktualisiert. | M |
| FA-4.05 | Die bevorstehenden Spiele werden in einer Kalenderansicht der aktuellen Woche dargestellt. | S |
| FA-4.06 | Die vollständige Ligatabelle eines Vereins ist auf Abruf (z.B. per Tap) einsehbar, nicht dauerhaft sichtbar. | C |

---

## 5 · Zitat des Tages

| ID | Anforderung | Prio |
|----|-------------|------|
| FA-5.01 | Das Dashboard zeigt täglich ein Zitat an. Die Zuordnung Datum → Zitat ist deterministisch (kein Wechsel bei Seitenreload). | M |
| FA-5.02 | Der Zitate-Pool umfasst 365 Einträge (Text + optionaler Autor), die in der Datenbank gepflegt werden. | M |

---

## 6 · HVV-Abfahrtsmonitor

| ID | Anforderung | Prio |
|----|-------------|------|
| FA-6.01 | Das Dashboard zeigt die nächsten Abfahrten für eine in `appsettings.json` konfigurierte Haltestelle an. | M |
| FA-6.02 | Pro Abfahrt werden mindestens Linie, Richtung und Abfahrtszeit (Minuten ab jetzt oder Uhrzeit) dargestellt. | M |
| FA-6.03 | Abfahrtsdaten werden automatisch regelmäßig aktualisiert (Push, kein manueller Reload). | M |
| FA-6.04 | Die Polling-Frequenz beträgt maximal 1 Request pro Minute pro Haltestelle (HVV-Schonung). | M |
| FA-6.05 | Bei nicht erreichbarem HVV-Endpoint zeigt die Kachel einen freundlichen „Daten gerade nicht verfügbar"-Zustand statt eines Fehlers. | M |
| FA-6.06 | Die Konfiguration der Haltestelle (Stations-ID + Linien-Filter) erfolgt ausschließlich in `appsettings.json`, nicht zur Laufzeit. | M |
| FA-6.07 | Das Dashboard kann gleichzeitig 1–3 Haltestellen anzeigen (analog zum HVV-Abfahrtsmonitor-Generator). | S |
| FA-6.08 | Für jede Abfahrt wird visuell gekennzeichnet, ob Echtzeitdaten vorliegen oder nur der Plan-Fahrplan. | C |
| FA-6.09 | Bei vorhandenen Echtzeitdaten wird die Verspätung in Minuten angezeigt. | C |
| FA-6.10 | Verkehrsmittel werden visuell unterschieden (z.B. Bus vs. Nachtbus, U-Bahn, S-Bahn) – etwa per Icon oder Farbcode. | C |

> **Hinweis:** Diese Funktionalität nutzt einen inoffiziellen JSON-Endpoint der HVV-Webseite. Details, Begründung und rechtliche Einordnung siehe `docs/hvv-api-notes.md`.

---

## 7 · Allgemein

| ID | Anforderung | Prio |
|----|-------------|------|
| FA-7.01 | Ein einfacher Admin-Bereich (separate Route, kein Login) ermöglicht das Verwalten der Zitate (CRUD). | C |
| FA-7.02 | Authentifizierung für den Admin-Bereich (einfaches Single-User-Passwort). | C |
| FA-7.03 | Das Dashboard ist auch auf anderen Geräten im lokalen Netz responsiv nutzbar (nicht nur iPad). | C |

---

## 8 · Lauf-Heatmap (Erweiterung, Phase 7)

> Optionales Zusatz-Feature, das nach Abschluss aller Kernphasen (0–6) implementiert wird. Visualisiert die eigenen Lauf-Activities aus Strava als geografische Heatmap auf einer separaten Route, erreichbar via Tap im Habit-Bereich.

| ID | Anforderung | Prio |
|----|-------------|------|
| FA-8.01 | Lauf-Activities werden via offizieller Strava-API automatisch in die lokale Datenbank synchronisiert. | M |
| FA-8.02 | Die Authentifizierung gegen die Strava-API erfolgt via OAuth2; Access- und Refresh-Token werden sicher serverseitig persistiert. | M |
| FA-8.03 | GPS-Tracks werden in Postgres mit aktivierter PostGIS-Extension als geographische Datentypen (`geometry(LineString, 4326)`) gespeichert. | M |
| FA-8.04 | Die Heatmap-Ansicht ist über eine separate Route erreichbar (z.B. `/heatmap`) und wird per Tap im Habit-Bereich des Dashboards aufgerufen. | M |
| FA-8.05 | Die Karten-Darstellung erfolgt clientseitig mit Leaflet inkl. einer Heatmap-Layer (z.B. `Leaflet.heat`). | M |
| FA-8.06 | Nur Lauf-Activities (Strava-Typen `Run`, `TrailRun`) werden in die Heatmap aufgenommen; andere Activity-Typen werden ignoriert. | M |
| FA-8.07 | Beim erstmaligen Setup werden alle historischen Lauf-Activities geladen; spätere Syncs sind inkrementell (nur Activities seit dem letzten erfolgreichen Sync). | M |
| FA-8.08 | Der Sync-Service respektiert die Strava-API-Rate-Limits. Bei Erreichen wird der Sync pausiert, nicht abgebrochen. | M |
| FA-8.09 | Bei nicht erreichbarer Strava-API oder ungültigem Token zeigt die UI den letzten erfolgreichen Sync-Zeitstempel an. | M |
| FA-8.10 | Die Heatmap-Ansicht enthält einen Zeitraum-Filter (mindestens: letzte 4 Wochen, letzte 12 Monate, alle). | S |
| FA-8.11 | Aggregierte Lauf-Statistiken für den gewählten Zeitraum (Gesamt-Kilometer, Anzahl Runs, Durchschnitts-Pace) werden über bzw. neben der Karte angezeigt. | S |
| FA-8.12 | Zusätzliche Heatmap-Layer (Pace, Herzfrequenz) sind via UI-Toggle umschaltbar. | C |
| FA-8.13 | Activities können auf der Karte einzeln angeklickt werden, um Detail-Infos (Datum, Distanz, Pace, HR) anzuzeigen. | C |

> **Hinweis:** Diese Funktionalität nutzt die offizielle Strava-API und erfordert ein einmaliges OAuth-Setup durch den Eigentümer. Details zur API, Rate-Limits, Token-Handling und Datenmodell siehe `docs/strava-api-notes.md` (wird in Phase 7 angelegt).