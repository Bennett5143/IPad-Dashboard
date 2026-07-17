# PRD — e-ink / Papier Redesign

> **Status:** approved (Design-Session mit Moodboard, Rev 5). Umsetzung in kommender Session.
> **Art:** UI-/Layout-/Navigations-Refactor im bestehenden Blazor-Projekt — **kein Rewrite**.
> **Moodboard-Referenz:** Artifact „e-ink Dashboard — Home (Rev 5)".
> **Konzept-Historie:** `~/.claude/plans/moin-du-befindest-dich-dreamy-rocket.md`.

## 1 · Kontext & Ziel

Der aktuelle Look („Command Center", dunkel + 5 Domänenfarben, seit PR #107) fühlt sich „teils
falsch" an. Inspiriert von **TRMNL** (e-ink-Terminal) wollen wir ein **ruhiges, papierartiges
Info-Blatt**: hoher Kontrast, striktes Raster, Typo-first, viel Weißraum, statisch, kein
Farb-Rauschen. Die Disziplin von e-ink ist das Ziel — nicht das Monochrome an sich.

**Leitprinzipien**
- **Farbe = Information, nie Dekoration.** Ein Primär-Akzent (Waldgrün) für Links/aktive Auswahl;
  sonst nur Zustandsfarbe an Datenpunkten (WHOOP-Recovery-Ampel, Bull/Bear, Heatmap-Route).
- **Struktur durch Haarlinien & Raster, Textur durch 1-bit-Dither**, keine Schatten/Gradienten.
- **Statisch & ruhig** (passt auch zur schwachen iPad-6-GPU). Einzige Bewegung: die Analoguhr-Zeiger.
- **Blazor Server bleibt** — passt perfekt zum Offline-iPad (Server hält State + hat Internet).

## 2 · Design-System / Tokens

Kern ist ein **`:root`-Token-Tausch in `src/Dashboard.Web/wwwroot/app.css`** (wie beim Waldgrün-
Re-Theme 15.2), plus Komponenten-CSS. Zwei Themes über dieselben Tokens, **harte Umschaltung**:

| Token | Tag `data-theme="eink"` | Nacht `data-theme="night"` |
|---|---|---|
| `--bg` (Grund) | `#f5f2eb` | `#121210` |
| `--ink` (Text) | `#23201a` | `#ece6d8` |
| `--muted` | `#6f6a5d` | `#928b7c` |
| `--faint` (Dither/Grain) | `#a9a291` | `#4c483f` |
| `--rule` (Haarlinie) | `#cbc3b1` | `#33302a` |
| `--track` (Ring-/Event-Füllung) | `#e4dece` | `#26241f` |
| `--accent` / `--good` (Waldgrün) | `#2f8256` | `#57c98a` |

- **Datentragende Farben außerhalb der Ramp:** Heatmap-Route Amber `#ff9628`, WHOOP-Recovery-Ampel
  (grün/gelb/rot nach Zone, für Papier leicht entsättigt), Bull/Bear-Vorzeichen.
- **Typo:** JetBrains Mono (Daten/Labels/Zeiten) + Space Grotesk (Text/Überschriften) — beide in-app
  bereits geladen. Mono ist der „Star".
- **Textur:** Dither via CSS `radial-gradient(var(--faint) .5px, transparent .6px); background-size:3px 3px`.
- **Flächen:** flache Kacheln (`--tile-*`-Gradienten weg), 1px `--rule`-Rahmen, Radius klein (~6px),
  `--shadow-*` → none. Alte `--glass-*`/`--glow-*`-Shims entfernen.

## 3 · Home-Seite (Detail-Spec)

Bento-Layout, No-Scroll (1024×768), **keine Kachel-Überschriften** (bis auf Monatstitel im Kalender):

- **Oben links (schmale Spalte):** kleine **Analoguhr** + Digitalzeit `14:32` (**kein Datum**) +
  **3 WHOOP-Ringe**: Recovery **farbig** (Zonen-Ampel), Strain + Sleep in **Tinte**.
- **Oben rechts:** **Wetter** — aktuell (Glyph, große Temp, Beschreibung, Gefühlt/Hoch-Tief/Wind),
  **Sonnenuntergang klar beschriftet** (Sunset-Icon + Label + Zeit). Darunter **Stundenkarten
  nebeneinander**, je Karte: **Zeit → Wetter-Icon → Temp → Regen%** (Regen ≥30 % in Tinte).
  → **Präfixe:** Höchsttemperatur mit **„H"**, Tiefsttemperatur mit **„T"** (z. B. `H 21° / T 12°`).
- **Unten (volle Breite):** **Kalender** = **Mini-Monatsraster** (heute im Akzent, Event-Punkte an
  belegten Tagen) + **Stunden-Tagesplan** (Timeline mit Stundenlinien; Termine als Blöcke an echter
  Zeit/Dauer; **Lücken = freie Zeit sichtbar**). Timeline-Fenster idealerweise dynamisch (ab „jetzt"
  bzw. erster–letzter Termin +Puffer), nicht fix 08–22.
- **Status-Health-Icon:** in die **Rail** verschoben, **entfärbt** (Tinte/muted). `up` = Pulslinie,
  `down` = unterbrochene Flatline (an der **Form** erkennbar). Link → Status-Seite.
- **Icon-Rail (unten, reine Icons, kein Text):** Bus · Hantel · Fußball · Bitcoin · Health.
  Neue Icons: **Bus frontal**, **Hantel dick**, Fußball, Bitcoin, Puls/Flatline, Wetter-Set
  (sonnig/wolkig/bedeckt/Regen/Sonnenuntergang).

## 4 · Navigationsmodell — Hub & Spoke

- **Keine globale Navbar mehr** zwischen Unterseiten (kein Sprung Fußball → Lauf-Heatmap).
- **Fünf isolierte Bereiche**, jeder nur von Home erreichbar, zurück immer über Home:
  **HVV · Fitness (enthält `/heatmap`, `/runs`, `/habits`, `/whoop`) · Fußball · Crypto · Status.**
- Rail = die 4 Daten-Icons; **Status** hängt am Health-Icon.
- Bestehende globale Navigation (`KioskLayout`/NavMenu) entsprechend umbauen.

## 5 · Subpages — Neu-Design im e-ink-Stil (WICHTIG)

**Alle Unterseiten müssen in denselben Stil überführt werden**, nicht nur Home:
`/hvv`, `/weather`, `/football`, `/crypto`, `/status`, `/whoop`, `/runs` (+ `/runs/{id}`),
`/habits`, `/heatmap`. Konkret je Seite: Papier-Tokens, Haarlinien/Tabellen-Look, Mono-Zahlen,
Akzent nur für Zustand, flache Flächen, Dither wo Füllung nötig. Bestehende wiederverwendbare
Komponenten (`Tile`, `StandingsTable`, `KnockoutBracketView`, `MetricRing`, `Sparkline`,
`ChartFrame`, Insights-Karten) auf die neuen Tokens ziehen.

**Lauf-Heatmap muss WEISS/hell werden.** Aktuell ist die Karte dunkel: dunkler CSS-Filter
(`invert hue-rotate brightness contrast`) auf `::deep .leaflet-tile-pane` + Container-BG `#16181d`
(siehe `Heatmap.razor.css` / `wwwroot/js/heatmap.js`). Für e-ink:
- Dunkel-Filter **entfernen** (helle OSM-Kacheln so lassen) **oder** einen dezenten Papier-Filter
  (leichte Entsättigung/Wärme) setzen, damit die Karte zum Papier-Grund passt.
- `::deep .leaflet-container { background: var(--bg) }` (statt `#16181d`).
- **Route bleibt Amber `#ff9628`** (Daten). Kontrast auf hellem Grund prüfen; ggf. Strichbreite/
  Deckkraft anpassen.
- `?v=N`-Cache-Bust in `Heatmap.razor` hochzählen (JS/CSS-Änderung).

## 6 · Apple Calendar (neue Datenquelle, eigener Slice)

Home-Kalender braucht echte Termine. Machbar server-seitig (Pi hat Internet):
- **iCloud CalDAV** (App-spezifisches Passwort) **oder** veröffentlichte **`.ics`-Abo-URL**.
- Server holt + cached (wie Tiles/Crests), iPad zeigt nur die fertige Agenda + Monatsraster.
- Config/Secrets in `appsettings.Local.json` (nicht versioniert), Muster wie WHOOP/Strava-Tokens.
- **Eigener Feature-Slice**, nicht in den Re-Theme mischen. Erst mit Dummy-Daten bauen, dann anbinden.

## 7 · Umsetzungs-Reihenfolge (Slices, je 1 Branch → 1 Squash-PR)

1. **Tokens:** Papier- + Nacht-Palette in `app.css` hinter `data-theme` (Command Center als Fallback lassen).
2. **Home-Spike:** neues Bento-Home hinter `data-theme="eink"` bauen → **am echten iPad ansehen** (A/B).
3. **Nav-Refactor:** globale Navbar raus, Hub-&-Spoke, Icon-Rail + Health-Status.
4. **Subpages** re-themen (je Seite/kleiner Batch; gemeinsame Komponenten zuerst).
5. **Heatmap** auf hell/weiß.
6. **Kalender-Slice** (CalDAV/ICS).
7. **Nacht-Theme** + abendliche Umschaltung finalisieren.
8. **Aufräumen:** alte Command-Center-/Glass-Tokens + toter CSS entfernen.

## 8 · Verifikation

`dotnet run --launch-profile https --project src/Dashboard.Web`, am iPad 6 (1024×768 quer, LAN)
prüfen: No-Scroll hält, Kontrast/Lesbarkeit (Analoguhr, Stundenkarten, Timeline), Timeline-Höhe
passt (5 Termine + Stundenlinien), Heatmap hell + Route lesbar, Nacht-Umschaltung sauber.

## 9 · Offene Entscheidungen

- Timeline-Fenster **dynamisch vs. fix**.
- Kalender endgültig **ja/nein** (aktuell: ja, Dummy im Moodboard).
- Radius-/Dither-Intensität final tunen.
- Nacht-Umschaltung **manuell/Uhrzeit/Sonnenuntergang-getrieben**.

## 10 · Betroffene Dateien (Anhaltspunkte)

- `src/Dashboard.Web/wwwroot/app.css` (Tokens, Dither-Utility, Themes)
- `Components/Tile.razor(.css)` (flach, Domänen-Farbe raus)
- `Home.razor(.css)`, `KioskLayout.razor(.css)` (Bento, Rail, Nav-Umbau)
- Wetter-/Kalender-Komponenten (Stundenkarten, Monatsraster, Tages-Timeline — teils neu)
- `Heatmap.razor.css`, `wwwroot/js/heatmap.js` (heller Kartenstil)
- gemeinsame Komponenten: `StandingsTable`, `KnockoutBracketView`, `MetricRing`, `Sparkline`, `ChartFrame`
- neu: `Infrastructure/Calendar/*` (CalDAV/ICS-Client + Cache), Options in `appsettings.Local.json`
