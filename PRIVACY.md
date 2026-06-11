# Privacy Policy — iPad Kiosk Dashboard

_Last updated: 2026-06-11_

This is a **private, self-hosted, single-user** dashboard application run by the
individual who owns it. It is **not a commercial product** and is not offered to
other users.

## What data is accessed

When connected to third-party services via their official APIs, the app reads
**only the account owner's own data**:

- **WHOOP** (via the WHOOP Developer API): recovery, sleep, physiological cycle /
  strain, workout and basic profile data, using read-only scopes
  (`read:recovery`, `read:sleep`, `read:cycles`, `read:workout`, `read:profile`).
- **Strava** (optional): the owner's own activities.
- Public weather, public-transport and football data (no personal data).

## How data is stored and used

- All data is stored **locally**, in the owner's own PostgreSQL database on the
  owner's own device/home server. OAuth access/refresh tokens are kept
  **server-side only** and never displayed or transmitted to anyone.
- Data is used **solely** to display the owner's own metrics on the owner's own
  screen. It is **not shared** with any third party, **not sold**, and **not** used
  for advertising or for training/operating any AI/ML system.

## Retention and deletion

- Data lives only in the local database and is removed when the owner deletes it
  or tears down the deployment.
- Access can be revoked at any time in the WHOOP app under **Settings → Connected
  Apps** (and equivalently in Strava settings), which invalidates the tokens.

## Contact

For questions about this personal project, open an issue at
<https://github.com/Bennett5143/IPad-Dashboard>.
