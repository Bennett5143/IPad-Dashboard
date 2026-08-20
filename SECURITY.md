# Security Policy

This is a personal dashboard that runs as a kiosk on a LAN-only iPad; the
deployment has no internet-facing surface, which is why it deliberately uses
plain HTTP and no authentication (see [docs/architecture.md](docs/architecture.md)).

The supply chain is monitored automatically: CodeQL, Grype image scans,
NuGetAudit, and OpenSSF Scorecard report into this repository's Security tab,
and every published image carries an SBOM attestation.

## Reporting a vulnerability

If you spot a security issue in this repository, please report it via
[GitHub Security Advisories](../../security/advisories/new)
("Report a vulnerability"). Reports are read and answered on a best-effort
basis — this is a hobby project.
