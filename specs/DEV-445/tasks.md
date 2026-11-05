# Tasks: Collection Filter TMDb Enablement

**Input**: `specs/DEV-445/` spec.md, plan.md, frozen brief.md (Q1–Q5) plus Q6 amendment, CONCLUSIONS.md, ASSUMPTIONS.md, recon-DEV-445, recon-DEV-445-2, note DEV-445, specs/PRODUCT.md

**Status**: PLAN FROZEN — Keel Phase 2 step 6 adjudication verified 2026-11-04; see brief.md / CONCLUSIONS.md final freeze receipt and adjudication-DEV-445. Gate 1 remains CLOSED pending Patron provisional review, Rigger spec PR, and user merge. No implementation clearance.

**Gate 1**: No owner checkbox (Q5). Handoff chain: **Patron provisional Gate 1 → Rigger opens the spec PR → user merges Gate 1 → Keel T000** records the verified merge receipt in `CONCLUSIONS.md`. Phase B pickup and implementation MUST NOT start before that receipt (Q5).

**Tests**: Required (brief Q4; reconciled Q6 / Ledger S-10). Backend TDD remains optional; identity unit proofs (T003) before production identity edit (T005), red where feasible; existing green disabled-supplement / fallback / persistence / resilience / chain-failure locks stay green — do not require a recorded failing run for already-correct branches. Web-implement red→green is mandatory for T009→T010→T011. No reading `.env` or real secrets in tests. Live Compose Bond acceptance is the single operational T020 sequence and is independent of mocked passes.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`.

**Conventions**: `Method_Condition_Expected`; `TestContext.Current.CancellationToken`; only `// arrange` / `// act` / `// assert`; no explanatory code comments; do not delete or rewrite existing comments/docstrings unrelated to the change. No new packages/layers/API/schema; never print/commit secrets; do not edit files outside the brief allowlist without a cited decision. Mutation hygiene: literals are `const`, no new static initialisers. Tests use `FakeTimeProvider` / deterministic `TimeProvider` and seeded `Random` when needed — no `DateTime.Now`, `Task.Delay`, or wall-clock waits.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement Phase N` dispatch (task-pipeline Phase 3 step 2). They are **not** canonical `task-pipeline` phases. Canonical pipeline remains Phase 3 Build/refactor/gates, Phase 4 review, Phase 5 delivery. Seat map: Wisp/Keel/Gauge retain Pickup (local Phase 1); Anvil code phases 2–4; Cog local Phase 5; Gauge local Phase 6–7; local Phases 8–9 map to canonical review/delivery Phase 4/5.

---

## Local phase dependency / execution order

```text
Phase 0  T000 (Keel Gate 1 record) — blocks T001+
Phase 1  T001 (Wisp drift recon; Keel reanalysis; Gauge baseline) — blocks T002+
Phase 2  T002 → T005   (Anvil; US1–US2 config + identity)
Phase 3  T006 → T008   (Anvil; US2 composed/persistence)
Phase 4  T009 → T011   (Anvil; US3 web-implement)
Phase 5  T012          (Cog /refactor once — required stage; edits may be conditional)
Phase 6  T013 → T019   (Gauge — canonical Phase 3 steps 5–6)
Phase 7  T020          (Live Compose Bond — US4)
Phase 8  T021          (canonical Phase 4 — Sentry/Ledger/Compass; Keel adjudication; Cog remediation)
Phase 9  T022 → T023   (canonical Phase 5 merge bar)
```

---

## Phase 0: Gate 1 record (blocks Phase 1)

**Purpose**: Confirm user Gate 1 merge before any Build work.
**Checkpoint**: Verified merge receipt in CONCLUSIONS.md; or stop.
**Handoff note**: Patron provisional → **Rigger** opens the spec PR → user merges Gate 1 → **Keel** T000 (brief Q5).

- [X] T000 **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-445 in `specs/DEV-445/CONCLUSIONS.md`. Receipt only — does not create a new ruling. T001–T023 MUST NOT proceed until this receipt exists (Q5; FR-013).

---

## Phase 1: Pickup — fresh recon, drift reanalysis, Gauge baseline (blocks implementation)

**Purpose**: Confirm frozen brief/spec/plan/tasks still match landed main and the approved file/contract set; obtain Gauge baseline before edits.
**Checkpoint**: Fresh recon recorded; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift; unresolved drift stops for a cited decision — no invention. Baseline captured.

- [ ] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main for the approved production/test allowlist and contracts (do not treat Phase A recon baseline as current-state clearance). Confirm public API/OpenAPI/TS collection contracts remain unchanged and schema still needs no migration. **Keel** runs cross-artifact reanalysis of brief/spec/plan/tasks against landed main. Amend tasks only where authorized by demonstrated drift through the assigned workflow. **STOP** on unresolved drift or missing contract facts for a cited decision — do not invent substitutes. **Gauge** records pre-edit baseline. No production edits in this task (brief Q5; FR-010, FR-012).

---

## Phase 2: Config wiring and supplement identity (Anvil; Priority: P1 — User Stories 1–2)

**Goal**: Land static Compose/config proofs and the minimal IMDb supplement identity forwarding with matching unit regressions.
**Independent Test**: `TmdbComposeTests` green on exact file contents (including api.settings.example.json negative lock); chain unit cases red where new identity behavior is missing, then green after forwarding; existing disabled/fallback locks stay green without requiring a recorded failing run for already-correct branches.
**Order (Ledger S-5/S-10)**: T002 may parallel T003; T004 after T002; T005 after T003; shared chain test file and production chain file are sequential.

- [X] T002 [P] [US1] Add static non-secret configuration tests in `tests/LamuFlix.UnitTests/TmdbComposeTests.cs` asserting exact `docker-compose.yml` api env `Tmdb__Enabled: ${TMDB_ENABLED:-true}` and `Tmdb__ApiKey: ${TMDB_API_KEY:-}`, `.env.example` `TMDB_ENABLED=true` with empty `TMDB_API_KEY=`, `src/LamuFlix.Api/appsettings.json` `Tmdb.Enabled: false`, and a static negative lock that tracked `docker/api.settings.example.json` has no `Tmdb` section or `TMDB_API_KEY` material (file unchanged). Mirror `ObservabilityComposeTests` line-based active-line parsing and `Directory.Packages.props` root-marker walk (recon-DEV-445-2:44-49); no YAML package; no shared-helper extraction; never hardcode gate thresholds; never read `.env` or real secrets (FR-001–FR-003, FR-011; Q2, Q4, Q6 / Sentry F3 / Ledger S-2; SC-002)
- [X] T003 [US2] Add/extend unit proofs in `tests/LamuFlix.UnitTests/Adapters/MetadataProviderChainTests.cs` as four separate identity branches with `Method_Condition_Expected` naming, reusing existing `StubMetadataProvider` (no NSubstitute introduced to this suite): (1) manual only; (2) OMDb only; (3) both with distinct valid IDs so manual wins; (4) neither preserves original lookup instance/title/year. Also cover OMDb metadata retention, TMDb-only merge fields, `/find` miss → primary Found with Unavailable signals (no title retry), disabled supplement no call, and fallback Found-from-TMDb no repeated supplement call. Backend TDD optional; write tests before T005 identity edit; red where feasible for missing new identity behavior; keep existing green locks intact — do not require a recorded failing run for already-correct branches (FR-005–FR-007, FR-011; Q1, Q4, Q6 / Ledger S-4/S-6/S-10; SC-003)
- [X] T004 [US1] Wire Compose and docs together: set api environment in `docker-compose.yml` to exact Q2 `Tmdb__*` interpolations (`TMDB_ENABLED:-true`, empty key placeholder); document `TMDB_ENABLED=true` and empty `TMDB_API_KEY=` in `.env.example`; update `README.md` Run with Docker Compose for local TMDb `.env` key/enablement (TMDb secrets only in ignored `.env`), no committed credentials, `docker compose up -d` apply step, OMDb JSON channel distinction, and an explicit warning to set `TMDB_ENABLED=false` until a real `TMDB_API_KEY` is set when OMDb-only `NotFound` behavior is desired; leave `docker/api.settings.example.json` and `appsettings.json` Enabled:false unchanged; no registration edits (FR-001–FR-004, FR-010; Q2, Q6 / Sentry F1; SC-002)
- [X] T005 [US2] Implement minimal supplement lookup forwarding in `src/LamuFlix.Infrastructure/Adapters/ChainedMetadataProvider.cs` using `lookup.ManualImdbId ?? found.Metadata.ImdbId`; when the resolved id is null, do not rebuild the lookup — preserve the original lookup instance/title/year; do not redesign merge ownership or persistence; no unrelated helper/layer; literals `const` / no new static initialisers; private helpers only inside this allowlisted file if complexity requires; turn T003 green for new identity branches (FR-005–FR-007, FR-010; Q1, Q6 / Ledger S-4/S-6; SC-003)

---

## Phase 3: Composed lookup and persistence proofs (Anvil; Priority: P1 — User Story 2)

**Goal**: Prove WireMock `/find` path and existing persistence availability semantics through the composed handoff.
**Independent Test**: Composition/lookup suites green for exact `/find` request and zero title search on miss; persistence Available/KnownEmpty/Unavailable green (extend only if needed).

- [X] T006 [US2] Extend WireMock composition/lookup coverage in `tests/LamuFlix.IntegrationTests/MetadataProviderChainCompositionTests.cs` and `tests/LamuFlix.IntegrationTests/SecondaryMetadataProviderLookupTests.cs` for exact `/find` IMDb id + `external_source=imdb_id` request, details fetch with collection/signals handoff, zero title search on `/find` miss, and composed blank AND whitespace `ApiKey` coverage with `Enabled=true`: primary miss → `Failed(ProviderUnavailable)` (exact category); primary hit → `Found` with `Unavailable` signals (exact signals); zero TMDb HTTP in both cases. Retain existing resilience/quota and chain failure-matrix regressions as evidence. Do not claim or add a new end-to-end enrichment retry test outside the allowlist; no registration edits (FR-006, FR-011; Q1, Q4, Q6 / Sentry F1; SC-003)
- [X] T007 [US2] Confirm or extend only-as-needed persistence coverage in `tests/LamuFlix.IntegrationTests/EfMovieRepositoryCollectionTests.cs` for Available upsert/set, KnownEmpty clear membership retain row, Unavailable leave new membership unset and preserve any existing membership under composed OMDb+TMDb metadata handoff; skip edits if existing tests already prove the matrix (FR-008, FR-011; Q1, Q4, Q6; SC-004)
- [X] T008 [US1] [US2] Verification receipt (no empty commit): re-run T002–T007 targeted suites green after Phase 2–3 edits; keep existing facet/catalog/`collectionId` endpoint regressions and chain failure matrices green without API changes; record receipt evidence only (FR-011–FR-012; Q6 / Ledger S-11; SC-003–SC-004, SC-006)

---

## Phase 4: Hide healthy-empty Collection facet (Anvil; Priority: P1 — User Story 3)

**Goal**: Mandatory web-implement red→green TDD for healthy-empty hide with preserved error and active-selection behavior.
**Independent Test**: FilterPanel (and toolbar if needed) tests green for hide/populated/error/retain/clear paths; obsolete empty-legend assertion replaced (never weakened to pass).
**Order**: T009 → T010 → T011 strictly (web-implement red→green; Ledger S-10).

- [X] T009 [US3] Add failing web tests in `web/src/features/library/FilterPanel.test.tsx` (and `web/src/library/LibraryView.toolbar.test.tsx` only if active-selection/toolbar coverage requires it) for: Collection group omitted when `collections` empty and `!collectionFailed`, asserted by role/name absence query for Collection plus positive controls that other groups remain visible (Genres, Enrichment status, or Sort); group+error alert visible when `collectionFailed`; populated chips retain existing ordering/labels/copy; active `collectionId` retained while hidden; chip removal and Clear All still clear selection. Replace the obsolete empty-legend assertion that expected the group on empty collections — never weaken it to pass. Toolbar stale-chip tests must close the panel first per corrected design (chips render only when panel closed) (FR-009, FR-011; Q3–Q4, Q6 / Ledger S-3; SC-005; ASSUMPTIONS.md; design.md)
- [X] T010 [US3] Implement conditional Collection fieldset in `web/src/features/library/FilterPanel.tsx` so the group renders unless healthy-empty (`sortedCollections.length === 0 && !collectionFailed`); derive a const beside the existing `collectionFailed` derivation — no `useState`/`useEffect`; any extraction stays inside `FilterPanel.tsx` only; do not auto-clear URL/query `collectionId`; do not change populated copy/ordering; preserve Q3 render predicate and consume existing generated types only (FR-009–FR-010, FR-012; Q3, Q6 / T010 guidance)
- [X] T011 [US3] Verification receipt (no empty commit): re-run web-implement TDD loop to green for T009–T010; no other web production files; record receipt evidence only (FR-009–FR-011; Q6 / Ledger S-11; SC-005)

---

## Phase 5: Refactor (Cog — required stage; canonical Phase 3 step 3)

**Goal**: Cog runs `/refactor` once over the completed implementation; meet applicable refactor gates without changing behavior.
**Independent Test**: Prior unit/integration/web suites remain green.
**Note**: Stage remains mandatory even when edits are unnecessary. C# cyclomatic complexity / InspectCode apply only to changed C# (including the chain); FilterPanel TypeScript uses web lint/typecheck/Vitest via web gates — never apply C# tools to TypeScript (Ledger S-1). Any extraction stays inside allowlisted production files.

- [ ] T012 After implementation green: **Cog** runs `/refactor` once over the completed implementation (task-pipeline Phase 3 step 3). For changed C# (`ChainedMetadataProvider.cs`): meet cyclomatic complexity / InspectCode thresholds from harness.yml. For `FilterPanel.tsx`: satisfy web lint/typecheck/Vitest via web gates — do not run C# cyclomatic/InspectCode against TypeScript. Code edits apply only if the applicable gate requires them; the refactor stage itself remains mandatory even when no edits result. Preserve behavior; no speculative abstractions; unmet C# complexity gate prompts a new Patron ruling rather than allowlist expansion (Q5, Q6 / Ledger S-1; FR-010; FR-013)

---

## Phase 6: Configured gates (Gauge — canonical Phase 3 steps 5–6)

**Purpose**: Harness verification against the task diff; thresholds only from harness.yml; web gates when `/web` changed.

- [ ] T013 **Gauge** verification receipt (no empty commit): confirms frozen scope only — production/config/docs/web edits limited to allowlisted paths; no schema/migration/API/OpenAPI/TS/dependency/LocalPlay/Process.Start changes; no committed secrets; record receipt evidence only (FR-010, FR-012; Q5, Q6 / Ledger S-11; SC-006)
- [ ] T014 **Gauge** runs Roslyn analyzers on explicit changed `.cs` set via `./scripts/run-roslyn-analyzers.ps1` (FR-013; Q5)
- [ ] T015 **Gauge** runs `./scripts/run-cyclomatic-complexity.ps1` then the refactor threshold from harness.yml on changed C# files only — not TypeScript (FR-013; Ledger S-1)
- [ ] T016 **Gauge** runs `./scripts/run-jetbrains-inspectcode.ps1` on changed C# files (never `-All`; never TypeScript) (FR-013; Q5; Ledger S-1)
- [ ] T017 **Gauge** verification receipt (no empty commit): runs `dotnet format --verify-no-changes` clean; record receipt evidence only (authority: task-pipeline Phase 3 step 5; FR-013; Ledger S-11)
- [ ] T018 **Gauge** verification receipt (no empty commit): runs affected unit/integration suites then full `dotnet test`; record receipt evidence only (FR-011, FR-013; Ledger S-11; SC-003–SC-004)
- [ ] T019 **Gauge** runs property/vulnerable-packages gates per harness and `scripts/run-web-gates.ps1` / web-implement gates (lint/typecheck/Vitest) for changed `/web`; report SKIP/SKIPPED/N/A/Could not run distinctly — never as PASS; property exit 2 only with recorded task-note opt-out reason; mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` from the worktree for changed production C# (FR-013; Q5; SC-007)

---

## Phase 7: Live Compose Bond acceptance (Priority: P2 — User Story 4)

**Goal**: Single operational runtime sequence proving Compose TMDb default/override (US1) and Bond collection facet (US4) with operator credentials/media (Ledger S-7). Spec US1 acceptance and plan Runtime/Risks reference this task rather than repeating the full sequence.
**Independent Test**: Q4/Q6 receipt with expected/imported/matched Bond membership counts, sanitized quota/rate-limit evidence when applicable, and sanitized observed behavior (no secrets, no resolved secret-bearing Compose config); incomplete membership never passes; or explicit `Could not run` only for demonstrated environmental inability, with acceptance left open.

- [ ] T020 [US1] [US4] **Gauge** collects the single operational runtime Compose acceptance sequence after code gates, with **Conductor** environment coordination. Explicit checks, in order: (1) key-only default — local `TMDB_API_KEY` set in ignored `.env` and `TMDB_ENABLED` unset → enablement defaults true via sanitized observable behavior only (US1 acceptance #5; FR-001); (2) explicit false opt-out — set `TMDB_ENABLED=false` at runtime and verify TMDb supplement opt-out via sanitized observable behavior (brief Edge Case / Q2); (3) restore enabled Bond — restore enabled setup (`TMDB_ENABLED` unset or true with key still set), import Bond films in the running Compose stack; verify persisted collection data, visible James Bond Collection facet, and selecting it filters to those films (US4; SC-001; SMOKE-001). Record expected/imported/matched Bond membership counts plus sanitized environmental quota/rate-limit evidence; incomplete membership never passes (Q6 / Sentry F2). Retain environment, exact HEAD, timestamp, and sanitized before/after evidence for each check — never inspect, print, or record keys or resolved secret-bearing Compose config. A mocked pass does not close this criterion. Only demonstrated environmental inability (credentials/stack/media/quota/rate-limit) may be recorded as `Could not run` with acceptance left open — never silent waiver, fixture-only substitute, or reclassification of unexplained mismatch as environment (FR-001, FR-011, FR-013; Q4, Q6; brief.md:54; plan Runtime/Risks)

---

## Phase 8: Review (canonical Phase 4 — Sentry / Ledger / Compass; Keel adjudication; Cog remediation)

- [ ] T021 Size M code review with Sentry, Ledger, and Compass against Q5 closing bar (High/Critical block; every Medium/Low gets explicit cited disposition); **Keel** adjudicates; **Cog** remediates findings. Mandatory gates and live Bond acceptance block independently; max two review rounds and two fix commits per round; unresolved above-bar at cap blocks clearance and is surfaced on the PR — never silently waived or given a third immutable round; missing axis blocks; keep all review rounds visible on the PR; Compass checks `design.md` match for the Filter Panel change (FR-013; Q5; SC-007)

---

## Phase 9: Delivery / merge bar (canonical Phase 5)

- [ ] T022 **Rigger** performs fresh rebase onto latest default branch; **Gauge** re-runs applicable gates at current HEAD; **Conductor** coordinates merge-bar confirmation that watched hosted checks meet the standing merge bar (Q5)
- [ ] T023 **Conductor** hands off awaiting-merge: DEV-445 (#n); only the user merges (Q5)

---

## Dependencies

- T000 before T001; T001 before any T002–T023.
- T002 may proceed in parallel with T003 once T001 is done (`[P]`); T004 after T002 (config tests first); T005 after T003 (identity unit proofs before production identity edit; red where feasible; existing green locks stay green) and may follow T004 in the Q4 “tests + Compose/docs together” intent — do not merge production chain before T003 proofs exist; shared-file work stays sequential (Q6 / Ledger S-5/S-10).
- T006–T007 after T005; T008 after T006–T007 (verification receipt).
- T009 → T010 → T011 strictly (mandatory web-implement red→green).
- T012 after T008 and T011: Cog `/refactor` once is required even when no code edits result; C# tools on changed C# only; web gates for FilterPanel.
- T013–T019 after implementation + refactor stage evidence (T008/T011/T013/T017/T018 are verification/gate receipt steps — no empty commits).
- T020 after T013–T019 code gates (Q4–Q5); single operational sequence referenced from spec US1 and plan Runtime/Risks.
- T021 after T020 attempt (`Could not run` still allows review to proceed but clearance stays blocked until acceptance resolved or explicitly dispositioned per Q4 non-waiver).
- T022–T023 after review clearance path.

## Sequencing examples

```text
# Phase 0 / 1 (Patron provisional → Rigger spec PR → user merge → Keel / Wisp / Keel / Gauge)
T000 Keel Gate 1 merge receipt in CONCLUSIONS.md
T001 Wisp fresh recon + Keel drift reanalysis + Gauge baseline; STOP on unresolved drift

# Phase 2 config + identity (Anvil; backend TDD optional)
T002 TmdbComposeTests static asserts + api.settings.example.json negative lock (may [P] with T003)
T003 MetadataProviderChainTests four identity branches (tests before T005; red where feasible)
T004 docker-compose.yml + .env.example + README.md (blank-key opt-out warning)
T005 ChainedMetadataProvider.cs forwarding (no lookup rebuild on null) → T003 green

# Phase 3 composed/persistence (Anvil)
T006 WireMock /find + blank/whitespace-key composed proofs (zero TMDb HTTP)
T007 EfMovieRepositoryCollectionTests only if needed (Unavailable unset/preserve)
T008 verification receipt: targeted green + existing facet/browse/chain regressions

# Phase 4 web (Anvil; mandatory web-implement red→green)
T009 FilterPanel.test.tsx role/name absence + positive controls (toolbar close-panel first) red
T010 FilterPanel.tsx derived const beside collectionFailed (no useState/useEffect)
T011 verification receipt: web green

# Phase 5–9
T012 Cog /refactor once (C# tools on C#; web gates for FilterPanel; stage mandatory)
T013–T019 Gauge backend + web gates + mutation (receipt steps, no empty commits)
T020 Single operational runtime sequence (key-only / false opt-out / restore Bond + counts)
T021 Phase 8 review → canonical Phase 4
T022–T023 Phase 9 rebase/gates/awaiting-merge → user merges
```

## MVP

MVP for Gate 1 is Phase A documents only (plus Palette `design.md` before plan challenge). Build MVP after Gate 1 is T000–T011 (local Phases 0–4: config + identity + composed proofs + web hide). Full ticket delivery additionally requires T012–T020 (refactor + gates + live Bond) and T021–T023 (canonical Phase 4–5).

## Notes

- `[P]` only where different files and no incomplete deps; same-file edits stay sequential.
- Local `## Phase N:` IDs are for Anvil `/implement Phase N` dispatch (code phases 2–4) and seat routing; they are not task-pipeline phase numbers.
- Do not edit files outside the brief/recon frozen allowlist without a cited decision amendment (Q5). Missing facts → `needs recon` / `needs decision`, not invention.
- Surviving mutant = missing test; never lower thresholds (Q5).
- Quill does not run `/speckit-analyze` (Keel owns read-only analyze / plan challenge / freeze verification). Quill does not author `design.md` (Palette; design exists with verified Compass/Ledger S-8 corrections). Status is repaired for Keel freeze verification; Gate 1 remains closed. Quill does not declare PLAN FROZEN or implementation clearance.
- FR-013 / SC-007 retained with traceability; optional S-13 process trimming deferred (Q6 / Ledger S-9/S-13).
- Q6 Unavailable wording: leaves new membership unset and preserves any existing membership per Q4 — never clears it.
