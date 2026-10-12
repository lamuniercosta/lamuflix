# Requirements Checklist: DEV-321 — Library browse view

**Purpose**: Verify every spec requirement traces to the brief before Keel analysis.
**Created**: 2026-10-11
**Feature**: `specs/DEV-321/spec.md` (authority: `specs/DEV-321/brief.md`)

**Note**: L-ticket checklist (`/speckit-checklist`). Check items off as completed: `[x]`.

## Browse contract (FR-001–FR-007)

- [ ] CHK001 `MovieSummary` → `MovieSummaryResponse` widening preserves id/title/items/totalCount/route/params/status codes (spec FR-001; brief Q1).
- [ ] CHK002 `posterUrl` nullable passthrough; `releaseYear`/`runtime`/`imdbRating` match details names/types/units; nulls present as JSON null (FR-002–FR-005, FR-007).
- [ ] CHK003 `enrichmentStatus` required with exactly `Pending`, `Enriched`, `NotFound`, `Failed` in OpenAPI + generated TS; no global converter change (FR-006–FR-007).
- [ ] CHK004 No per-tile fetches, migration, or join in spec/plan/tasks (Edge Cases; brief Q1 stop-condition stated).

## URL + history (FR-008–FR-018)

- [ ] CHK005 Native History API only; adapter is sole owner; no router/state/validation dependency (FR-008).
- [ ] CHK006 Canonical keys + sort/direction vocabularies exact (FR-009); aliases read-only, never emitted, `genreIds` precedence, no other aliases (FR-010).
- [ ] CHK007 Always-emit keys; supplied pageSize honored; no size selector (FR-011).
- [ ] CHK008 Push/replace/popstate ownership + cleanup; page-reset rules (FR-012–FR-013).
- [ ] CHK009 Unknown-key drop; unparsable-to-default; server owns range validation (FR-014).
- [ ] CHK010 SPA defaults Title/Ascending/1/24; lists/groups/malformed/case/boolean/encoding rules (FR-015–FR-018).

## UI + fetching (FR-019–FR-028)

- [ ] CHK011 Supplied-poster grid with reserved-ratio title-labelled placeholder; seeded real-DB + RTL proof cited; no live-population claim (FR-019).
- [ ] CHK012 App integration: replace `App.tsx` + orphaned test; one provider; `index.css`; TanStack Query only (FR-020).
- [ ] CHK013 One normalized-key query via merged generated client with cancellation; no per-tile fetch/prefetch (FR-021).
- [ ] CHK014 Skeleton vs retain-content rules (FR-022); boundary + Retry + reset + 422-invalid-filter-never-retried / recoverable-including-400 handling with status-based classification (FR-023); ownership split (FR-024).
- [ ] CHK015 Root location; 2:3 grid; tile format + em dash; no play/detail/mutation controls (FR-025–FR-026).
- [ ] CHK016 Pagination `ceil(totalCount/pageSize)` + bounds + past-end behavior (FR-027); labels/keyboard/announcements (FR-028).

## Envelope, tests, gates (FR-029–FR-031)

- [ ] CHK017 Production file envelope exact (3 + 1 conditional); compile-forced sites only; regeneration paths; no hand-edited generated files (FR-029).
- [ ] CHK018 Backend real-DB + wire + schema tests named; web RTL/Vitest list named; conventions + no-new-package rules (FR-030).
- [ ] CHK019 Gates per Q9/Q10 (diff-scoped analyzers, mutation without `since`, property amendment, vulnerable-packages, format, `dotnet test`, web gates; thresholds from `harness.yml` only) (FR-031).
- [ ] CHK020 Out-of-scope items excluded: DEV-408 poster persistence, enrichment/worker repair, genre/actor picker, performance target, Phase B DEV-320 prerequisite stated not bypassed.

## Notes

- Traceability: each item cites its FR and brief question; FRs cite brief Q-lines.
- Taste wording (labels, copy, `min` suffix, one decimal, em dash) matches `ASSUMPTIONS.md`, not invented here.
- No `needs decision:` item — every spec statement rests on brief.md rulings or recon facts.
