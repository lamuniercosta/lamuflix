# Quickstart: DEV-561 outside-library discovery

**Authority**: `brief.md` Q10–Q11; `spec.md` smoke and US scenarios. Pickup confirms exact CLI/flags and seams.

## Prerequisites

1. Spec Gate 1 merged (this Spec Kit PR).
2. DEV-539 Done on the picked HEAD.
3. ADR 0049 Accepted (already in tree for review).
4. TMDb configured for enrichment/backfill (`Enabled` true, nonblank ApiKey). Discovery reads still return `[]` when unconfigured.

## Operator hydrate (legacy ID-only links)

After the additive migration is applied:

```text
# Exact operator invocation confirmed at Phase B pickup (existing DEV-539 mode)
dotnet run --project src/LamuFlix.Api -- --backfill-tmdb-recommendations
```

Use this to refresh matched source movies whose `movie_recommendations` rows still lack titles. Preserve prior links when a fetch fails.

## Local verification sketch

1. Apply migration against a database that already holds ID-only recommendation rows.
2. Run unit/integration suites for mapping, ranking, API zero-HTTP, For You parity, and web section behaviour.
3. **SMOKE-001 (required local seeded E2E):** with hydrated titled links and two eligible seeds for at least one outside candidate, open For You and confirm **Not in your library yet** with title, two-seed reason, and accessible TMDb link; library grid still loads. Fixture strategy confirmed at Phase B pickup.
4. **SMOKE-002 (both config-off cases):** with `Tmdb:Enabled` false, and separately with blank `ApiKey`, confirm API returns `200 []`, the discovery section is hidden, and the library grid remains usable (web tests cover API `200 []` → hidden section → usable grid).
5. **SMOKE-003:** on a visible discovery card, confirm no Play / library-details / Not interested controls, accessible new-tab TMDb link with `rel="noopener noreferrer"`, and day-verified TMDb credit beside the section.
6. For delivery evidence, run production ranking on the home library after hydration; publish ≥1-seed and ≥2-seed counts under the same Option A exclusion as the live query and owner verdicts on the ranked top 10 (or all if fewer). Keep raw viewing/seed data local.

## Notes

- Discovery HTTP path must not call TMDb.
- Do not invent Not interested, Wanted lists, or Related outside movies during verification.
