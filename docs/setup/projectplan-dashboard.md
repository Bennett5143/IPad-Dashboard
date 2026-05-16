# Projektplan: iPad-Kiosk-Dashboard

> Technische Rahmenplanung – noch ohne fachliche Domäne der Web-App.
> Dient als Grundlage für die weitere Detailplanung im Claude-Projekt
> und kann später ins GitHub-Wiki übernommen werden.

## Zielsetzung

Aufbau eines selbstgehosteten Kiosk-Dashboards, das auf einem iPad 6th Gen (2018, iPadOS 17.7.10, Safari)
im Vollbild läuft. Das iPad fungiert ausschließlich als **Anzeige-Client** – die gesamte Logik
und Datenhaltung liegt auf dem Raspberry Pi 4 (mit externer 256 GB SSD).

**Lernfokus** geht klar vor Produktivität: Das Projekt wird bewusst überengineert,
um möglichst viele Berührungspunkte mit modernen Backend-, DevOps- und Tooling-Themen zu schaffen.

## Tech-Stack

### Anwendungsschicht

- **Sprache:** C# (.NET 10.x)
- **Framework:** Blazor Server
  - Begründung: Realtime-Updates über SignalR "out of the box", kein WebAssembly-Overhead
    durch initialen Runtime-Download im Browser, einfacheres Deployment-Modell.
- **ORM:** Entity Framework Core
  - Provider: `Npgsql.EntityFrameworkCore.PostgreSQL`

### Datenbank

- **PostgreSQL** (Version 16, Alpine-Image)

### Containerisierung

- **Docker** + **docker-compose** für lokale Orchestrierung
- **Multi-Stage-Build** für das App-Image (SDK-Stage zum Bauen, Runtime-Stage als finales Image)
- **Named Volume** für Postgres-Daten (Persistenz unabhängig vom Container-Lebenszyklus)
- **`.env`-Datei** für Secrets/Credentials
- Trennung in mindestens zwei Services: `app` und `db`

### Hosting / Laufzeitumgebung

- **Raspberry Pi 4** mit externer SSD (256 GB) als Hostsystem
- iPad 6th Gen als Vollbild-Browser-Client (Safari-Kiosk-Modus)