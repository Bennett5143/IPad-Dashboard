# Security Policy

This is a personal dashboard that runs as a kiosk on a LAN-only iPad; the
deployment has no internet-facing surface, which is why it deliberately uses
plain HTTP and no authentication (see [docs/architecture.md](docs/architecture.md)).

The supply chain is monitored automatically: CodeQL, Grype image scans,
NuGetAudit, and OpenSSF Scorecard report into this repository's Security tab,
and every published image carries an SBOM attestation.

## Reporting a vulnerability

If you spot a security issue in this repository, please report it privately via
[GitHub Security Advisories](../../security/advisories/new)
("Report a vulnerability"). Please do not open a public issue for suspected
vulnerabilities.

## Disclosure process

- **Acknowledgement:** you can expect a first response within **14 days**
  (best-effort — this is a solo-maintained hobby project).
- **Assessment:** confirmed issues are fixed on `dev`, verified, and promoted to
  `main` like any other change; the advisory is updated along the way.
- **Disclosure:** once a fix is released, the advisory is published through
  GitHub Security Advisories, crediting the reporter (unless they prefer
  otherwise). If an issue is declined (e.g. it presumes an internet-facing
  deployment that this project explicitly does not have), the reasoning is
  shared in the advisory thread.

There is no bug-bounty program.
