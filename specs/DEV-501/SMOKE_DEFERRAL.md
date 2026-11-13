# DEV-501 — SMOKE-001..003 deferral record (T024)

Recorded by Anvil while implementing Phase 5 (US3). Facts only; it claims no runtime acceptance.

## Decision recorded

`tasks.md` T024 permits two outcomes: Playwright smoke specs in `web/e2e/` **only if a Compose-visible
path exists**, otherwise a documented smoke deferral against the OPEN runtime receipts. This file
records the deferral. No `web/e2e/` spec was added for `SMOKE-001..003`, and
`pwsh -NoProfile -File scripts/run-e2e-smoke.ps1` was not claimed as smoke proof for these items.

## Why no Compose-visible path exists

- The acquisition surface needs provider configuration: `Subtitles:OpenSubtitles` `ApiKey`,
  `BaseUrl`, `UserAgent`, `DownloadAllowedHosts`, `MaxDownloadBytes`, `RequestTimeout`
  (`src/LamuFlix.Core/Options/SubtitleProviderOptions.cs`).
- `SubtitleProviderOptionsValidator` requires `BaseUrl` to be an **absolute https URL** and requires at
  least one `DownloadAllowedHosts` entry; `SubtitleRequestUriPolicy` rejects any non-https scheme
  (`src/LamuFlix.Core/Options/SubtitleProviderOptionsValidator.cs`,
  `src/LamuFlix.Infrastructure/Adapters/SubtitleRequestUriPolicy.cs`).
- The e2e stub provider (`web/e2e/stub.mjs`) is a plain-http `127.0.0.1` server used for OMDb and
  Ollama only; the e2e settings written by `web/e2e/run.mjs` (`buildSettings`) contain no
  `Subtitles:OpenSubtitles` section at all.
- `docker/api.settings.example.json` has a `Subtitles` section with `SaveEnabled` and
  `PreferredLanguages` only: there is no working default provider key or https provider host to ship.
- A real OpenSubtitles key and https host therefore cannot be supplied by the e2e overlay, so
  `SMOKE-001` (ranked remote results), `SMOKE-002` (Get creates the Subs file and activates the track)
  and `SMOKE-003` (read-only / quota / rate-limit / timeout inline states against a live provider)
  cannot be reproduced on the Compose stack.

## What is proven instead

- `web/src/features/playback/subtitlesTab.test.tsx` (Vitest + RTL + MSW) covers the design.md state
  matrix against the generated contracts: catalog default/empty-preferences choose-language prompt,
  ordering and stale-search discard, quota known/unavailable copy, per-state status copy including
  distinct search-429 vs download-quota-429, stale-candidate 400, provider timeout/failure with manual
  Retry only, read-only `downloadAvailability.enabled === false` disabling Get without a POST,
  one-in-flight serialization, session-safe activation and changed-language completion, and D5
  (active indicator only on the active local track, never on a candidate row).
- API-level status/error mapping for the same states is proven on the backend by the US1/US2
  integration matrices (T015, T017, T020).

## Still OPEN

`SMOKE-001..003` and the home-cluster / BS Player receipts remain OPEN runtime acceptance per
`spec.md:75` and T032. They are `Could not run`, never PASS, until executed on the home cluster with a
configured provider key.
