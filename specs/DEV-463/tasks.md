# Tasks: Embedded Trailer on Movie Details (pt-BR captions preferred)

**Input**: `specs/DEV-463/` spec.md, plan.md, frozen brief.md (Q1–Q8), CONCLUSIONS.md, ASSUMPTIONS.md, recon-DEV-463, recon-DEV-463-2, note DEV-463, specs/PRODUCT.md

**Status**: PLAN FROZEN — 2026-11-05. Owner Gate 1 spec PR #329 merged at 225edf49; T000 and T001 complete, with receipts and pickup dispositions in CONCLUSIONS.md. UI-tag evidence reconciled; Gauge baseline captured with scope-empty gates SKIPPED and dotnet test exit 0. Phase 2 (T002–T005) complete at ea3ca388; Phase 3 (T006–T009) complete; Phase 4 (T010–T013) complete; Phase 5 (T014–T016) complete. Next: Cog Phase 6 T017. Earlier analyze/challenge receipts remain Phase A evidence; implementation verification and runtime acceptance remain outstanding.

**Gate 1**: No owner structural checkbox (brief Status; Q8; PRODUCT.md §5). Handoff chain: **Patron provisional Gate 1 → Rigger opens the spec PR → user merges Gate 1 → Keel T000** records the verified merge receipt in `specs/DEV-463/CONCLUSIONS.md`. Phase B pickup and implementation MUST NOT start before that receipt (Q7).

**Tests**: Required (ticket Acceptance; Q7). Prefer new-behavior regressions red before production edits where feasible. Green locks for existing enrichment failure/cancellation/OMDb-primary short-circuit stay green. TDD optional; required coverage green before verification. No live YouTube required for unit/MSW; runtime embed is separate evidence.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`.

**Conventions**: `Method_Condition_Expected`; `TestContext.Current.CancellationToken`; only `// arrange` / `// act` / `// assert`; no explanatory code comments; do not delete or rewrite unrelated comments. Existing xUnit v3 / Shouldly / NSubstitute / WireMock.Net / Testcontainers / Verify / Vitest / RTL / MSW only; no new packages/ports/layers; never hand-edit generated OpenAPI/TS/MSW/migration snapshots; never lower harness thresholds; TimeProvider / FakeTimeProvider for time; selection literals are `const`; no mutable static initialisers that bake mutant state. Targeted new test files within existing test projects are allowed under Q8; T001 identifies the projects and conventions.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement Phase N` dispatch. They are **not** canonical `task-pipeline` phases. Canonical pipeline remains Phase 3 Build/refactor/gates, Phase 4 review, Phase 5 delivery. Seat map: Wisp/Keel/Gauge Pickup (local Phase 1); Anvil code phases 2–5; Cog local Phase 6; Gauge local Phase 7; local Phases 8–9 ↔ canonical Phase 4; local Phase 10 ↔ canonical Phase 5.

---

## Local phase dependency / execution order

```text
Phase 0   T000 (Keel Gate 1 record) — blocks T001+
Phase 1   T001 (Wisp drift recon; Keel reanalysis; Gauge baseline) — blocks T002+
Phase 2   T002 → T005   (Anvil; US1 domain + EF)
Phase 3   T006 → T009   (Anvil; US1 TMDb + chain)
Phase 4   T010 → T013   (Anvil; US2 DTO + details JSON + OpenAPI + generate:api)
Phase 5   T014 → T016   (Anvil; US3 Trailer UI + MSW)
Phase 6   T017          (Cog /refactor once)
Phase 7   T018 → T023   (Gauge gates)
Phase 8   T024          (US4 runtime + PR CSP note)
Phase 9   T025          (canonical Phase 4 review)
Phase 10  T026 → T027   (canonical Phase 5 delivery)
```

---

## Phase 0: Gate 1 record (blocks Phase 1)

**Purpose**: Confirm user Gate 1 merge before any Build work.
**Checkpoint**: Verified merge receipt in CONCLUSIONS.md; or stop.
**Handoff note**: Patron provisional → **Rigger** opens the spec PR → user merges Gate 1 → **Keel** T000 (brief Q7).

- [X] T000 **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-463 in `specs/DEV-463/CONCLUSIONS.md`. Receipt only — does not create a new ruling. T001–T027 MUST NOT proceed until this receipt exists (Q7; FR-014).

---

## Phase 1: Pickup — fresh recon, drift reanalysis, Gauge baseline (blocks implementation)

**Purpose**: Confirm frozen brief/spec/plan/tasks still match landed main and the Q8 file/contract set; resolve unspecified entity/projection/copy-site filenames with evidence.
**Checkpoint**: Fresh recon recorded; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift; unresolved drift stops for a cited decision — no invention. Baseline captured.

- [X] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main for the Q8 bounded surfaces (`MovieMetadata` + copy sites, `TmdbMetadataProvider` (+ Mapping/partials), `ChainedMetadataProvider`, `EfMovieRepository`, `MovieConfiguration`, movie persistence entity/read projections, `LibraryResponses` / details mapping, OpenAPI/Verify/generate:api workflow, `MovieDetailsView`/CSS, movie-details fixtures/tests, `CONTEXT.md`). Name any unspecified entity/projection/copy-site files with evidence — do not invent shapes (Q8). **Keel** runs cross-artifact reanalysis of brief/spec/plan/tasks against landed main. Amend tasks only where authorized by demonstrated drift. **STOP** on unresolved drift or missing contract facts for a cited decision. **Gauge** records pre-edit baseline. No production edits in this task (brief Q7–Q8; FR-015).

---

## Phase 2: Domain Trailer key and nullable EF storage (Anvil; Priority: P1 — User Story 1 foundation)

**Goal**: Nullable Trailer key on `MovieMetadata`, copy/clear paths, EF column/migration/readback.
**Independent Test**: Domain/unit proofs for copy/clear; Testcontainers null/present roundtrip and re-enrichment clear/replace.
**Order**: T002 → T005 (shared domain/persistence surfaces; keep sequential where files overlap).

- [X] T002 [P] [US1] Add failing unit coverage for Trailer key copy/merge/clear and validation boundary (`^[A-Za-z0-9_-]{11}$`) in the existing domain/metadata unit suite (exact existing project and file named by T001 recon); cover null default for existing-shaped metadata (FR-001–FR-003; Q1–Q2; SC-004)
- [X] T003 [US1] After T001 recon: add nullable `TrailerKey` to `src/LamuFlix.Core/Domain/MovieMetadata.cs` and update necessary metadata-copy paths named by recon so the value carries through copy/merge/clear; own the single C# exact 11-character ASCII letter/digit/underscore/hyphen validator as a method on MovieMetadata, reused by the adapter/selector (no mutable static regex); store only validated keys, never URLs; land the pending CONTEXT.md Trailer key glossary append in this implementation task (FR-001–FR-003; Q1–Q2)
- [X] T004 [US1] Add nullable `trailer_key` mapping in `src/LamuFlix.Infrastructure/Persistence/Configurations/MovieConfiguration.cs` and tool-generated EF migration + model snapshot; wire write/read/clear through `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` and recon’d persistence entity/read projections; never hand-edit the snapshot (FR-002–FR-003; Q1)
- [X] T005 [US1] Add real-Postgres Testcontainers coverage proving null existing rows, present-key roundtrip, and re-enrichment replace/clear of Trailer key; make T002 green for domain/copy assertions (FR-012; Q7; SC-004)

---

## Phase 3: TMDb retrieval, selection, and chain merge (Anvil; Priority: P1 — User Story 1)

**Goal**: Documented dual language videos requests, eligibility/ranking, optional failure degradation, OMDb-primary supplement Trailer key.
**Independent Test**: WireMock/unit selection matrix + chain composition cases green.

- [X] T006 [P] [US1] Add failing enrichment/selection coverage (unit and/or WireMock in existing adapter/lookup suites after T001 recon) for: pt-BR vs original order; nonofficial pt-BR vs official original; official within tier; ordinal key ties; input-order independence; duplicate keys; missing original_language; empty results; wrong site/type; blank/malformed/URL-shaped keys; partial/all videos failures; videos-only 429/quota/timeout with no enrichment retry or DeadLetter and retained independent successes; details-path 429 existing retry behavior; pt/PT/pt-BR casing variants making exactly one videos request; PT/Br and YOUTUBE/TRAILER comparisons; cancellation before the second request; FsCheck permutation-invariance and dedupe-idempotence properties using existing infrastructure (FR-005–FR-008; Q3–Q4, Q7; SC-004)
- [X] T007 [US1] Extend `src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.cs` and `TmdbMetadataProvider.Mapping.cs` (plus recon’d partials if needed): read `original_language` from existing movie-details payload; after identity resolve request `/movie/{id}/videos?language=pt-BR` and `language=<original_language>` only when trimmed original language is nonblank and neither pt nor pt-BR under OrdinalIgnoreCase; language/site/type comparisons use OrdinalIgnoreCase while keys remain ordinal; union candidates; apply FR-007/FR-008 selection; documented `language` only; videos failures → no candidates for that request without failing otherwise valid enrichment; pass one token into both requests, check cancellation before the second, never swallow OperationCanceledException; pure selector method stays in Mapping.cs and reuses MovieMetadata validator, HTTP fetch stays in provider; videos-only 429/quota/timeout never triggers enrichment retry or DeadLetter and retains other successes; literals `const` (FR-004–FR-008; Q2–Q4)
- [X] T008 [US1] Extend `src/LamuFlix.Infrastructure/Adapters/ChainedMetadataProvider.cs` so OMDb-primary supplement carries TMDb Trailer key without replacing unrelated OMDb scalars; TMDb-found path retains selected key; disabled/unconfigured TMDb yields null Trailer key (FR-004; Q2)
- [X] T009 [US1] Make T006 suites green; add/extend composition coverage for OMDb-primary Trailer propagation, TMDb-found path, disabled TMDb, and no-eligible-trailer null; failed TMDb supplement after OMDb Found preserves scalars and null key; green-lock videos-only 429/timeout isolation versus details-path 429 retry behavior; make selector FsCheck properties green with no property-test opt-out for these invariants; preserve green locks for existing failure/quota/cancellation short-circuit behavior (FR-004–FR-005, FR-012; Q7; SC-004)

---

## Phase 4: Details DTO, OpenAPI, generated client (Anvil; Priority: P1 — User Story 2)

**Goal**: Expose nullable `trailerKey` on existing metadata response; prove details JSON null/present; regenerate OpenAPI → TS/MSW.
**Independent Test**: Details JSON null/present; Verify OpenAPI drift; `npm run check:api`.
**Order**: T010 → T013 (DTO before details JSON proofs; then OpenAPI; then generate:api / fixtures).

- [X] T010 [US2] Extend `MovieMetadataResponse` in `src/LamuFlix.Api/Endpoints/LibraryResponses.cs` with nullable `trailerKey` mapped from `MovieMetadata.TrailerKey`; no new route or status codes (FR-001, FR-011; Q1)
- [X] T011 [US2] After T010: author and run explicit green-lock details-response regression tests in the existing API integration suite under `tests/` (exact file named by T001 recon — do not invent a filename or serialization convention) for persisted valid Trailer key and no Trailer key; assert existing `metadata` nesting and nullable field behavior for `trailerKey`, confirming and asserting the existing nullable JSON convention from T001; update affected details snapshots only through supported Verify workflow, never hand edits (FR-011–FR-012; Q7; SC-004; brief/plan details JSON null/present; spec US2 scenarios 1–2)
- [X] T012 [US2] Regenerate committed OpenAPI via the supported OpenAPI/Verify workflow confirmed by T001 recon, updating `web/src/api/openapi.json` and affected snapshots through that workflow (do not assume an env var or test-project path); never hand-edit baseline (FR-011; Q7; SC-006)
- [X] T013 [US2] From `web/` run `npm run generate:api` producing `web/src/api/types.ts` and `web/src/mocks/handlers.ts`; verify with `npm run check:api`; update `web/src/mocks/fixtures.ts` for null/present `trailerKey` using generated types only (FR-011; Q7; SC-006)

---

## Phase 5: Movie Details Trailer UI (Anvil; Priority: P1 — User Story 3)

**Goal**: Trailer section before Related Movies with Q5 iframe contract; hide when invalid; MSW component proofs.
**Independent Test**: Vitest/RTL/MSW present/absent/invalid matrices green.

- [X] T014 [P] [US3] Add failing Vitest/RTL/MSW coverage under `web/src/features/movie-details/` for present, absent, and invalid Trailer key: section visibility; placement before Related Movies; heading `Trailer`; iframe `title="Trailer for <movie title>"` including a hostile title containing quotes/brackets to prove safe rendering; `loading="lazy"`; src host/path/caption params; no autoplay; internet/captions note; responsive 16:9 (FR-009, FR-012; Q5, Q7; SC-001–SC-003, SC-005; ASSUMPTIONS)
- [X] T015 [US3] Implement Trailer section in `web/src/features/movie-details/MovieDetailsView.tsx` (and CSS; optional small Trailer component in the same folder): render only when `metadata?.trailerKey` matches `^[A-Za-z0-9_-]{11}$`; fixed `https://www.youtube-nocookie.com/embed/<key>?cc_load_policy=1&cc_lang_pref=pt&hl=pt-BR`; placement immediately before `RelatedMovies`; no YouTube SDK (FR-009; Q5, Q8; ASSUMPTIONS)
- [X] T016 [US3] Make T014 green; confirm generated-contract fixture compatibility and that invalid/null keys render no empty placeholder (FR-009, FR-012; SC-002, SC-005)

---

## Phase 6: Refactor (Cog — required stage; canonical Phase 3 step 3)

**Goal**: Cog runs `/refactor` once over completed C# implementation; meet refactor complexity gate without behavior change.
**Independent Test**: Prior enrichment/persistence/API suites remain green.
**Note**: Code edits may be conditional; stage itself is mandatory. Extraction stays inside Q8 allowlisted production files.

- [X] T017 After implementation green: **Cog** runs `/refactor` once over completed C# changes (task-pipeline Phase 3 step 3). Preserve behavior; private helpers only inside Q8 allowlisted production files; unmet complexity gate needs a new Patron ruling, not a new file (Q7; FR-014–FR-015)

---

## Phase 7: Configured gates (Gauge — canonical Phase 3 steps 5–6)

**Purpose**: Harness + web verification against the task diff; thresholds only from harness.yml.

- [ ] T018 **Gauge** confirms frozen scope only: edits limited to Q8 bounded file plan + generated artifacts + `CONTEXT.md` Trailer key; no new dependency/port/project/layer/preferences/CSP middleware/LocalPlay/secrets; no undocumented TMDb parameters (FR-015; Q8; SC-008)
- [ ] T019 **Gauge** runs `./scripts/run-roslyn-analyzers.ps1` on explicit changed `.cs` set (FR-014; Q7)
- [ ] T020 **Gauge** runs `./scripts/run-cyclomatic-complexity.ps1` then the refactor threshold from harness.yml on changed files (FR-014)
- [ ] T021 **Gauge** runs `./scripts/run-jetbrains-inspectcode.ps1` on changed files (never `-All`) (FR-014; Q7)
- [ ] T022 **Gauge** runs `dotnet format --verify-no-changes`; affected suites then full `dotnet test`; property/vulnerable-packages per harness; mutation `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` from the worktree; report SKIP/SKIPPED/N/A/Could not run distinctly — never as PASS (FR-014; Q7; SC-008)
- [ ] T023 **Gauge** runs applicable web-implement gates and `npm run check:api` / OpenAPI drift for generated artifacts (FR-011, FR-014; SC-006)

---

## Phase 8: Runtime evidence and CSP/internet PR note (Priority: P2 — User Story 4)

**Goal**: Working embed + blocked-network usability evidence; PR documents first third-party embed / CSP `frame-src` / internet dependency.
**Independent Test**: Runtime receipt or explicit `Could not run`; PR body carries CSP/internet note.

- [ ] T024 [US4] **Gauge** collects runtime evidence for SMOKE-001–003 / working embed and blocked-network Movie Details usability in the provisioned browser (Conductor environment coordination); iframe onLoad alone is insufficient; unavailable external access → record `Could not run`, never PASS. Include CSP/internet note in the delivery PR: first third-party embed; requires internet; deployments with CSP need `frame-src https://www.youtube-nocookie.com`; no global CSP middleware added (FR-010, FR-013; Q5, Q7; SC-007). Verify the **Trailer key** glossary append landed in `CONTEXT.md` in T003; no glossary write in this runtime task (FR-015; Q8)

---

## Phase 9: Review (canonical Phase 4 — Sentry / Ledger / Compass; Keel adjudication; Cog remediation)

- [ ] T025 Size M code review with Sentry, Ledger, and Compass against Q6 closing bar (zero unresolved Critical/High; every Medium/Low explicit disposition); frozen scope per brief; **Keel** adjudicates; **Cog** remediates. Mandatory gates and runtime evidence block independently; cap two analysis/fix rounds and two challenge/review/fix rounds per applicable loop; report unresolved blockers at cap, never waive or reset; no fix-commit count cap; missing axis blocks; keep rounds visible on the PR (FR-014; Q6–Q7; SC-008)

---

## Phase 10: Delivery / merge bar (canonical Phase 5)

- [ ] T026 **Rigger** performs fresh rebase onto latest default branch; **Gauge** re-runs applicable gates at current HEAD; **Conductor** coordinates merge-bar confirmation that watched hosted checks meet the standing merge bar (Q7)
- [ ] T027 **Conductor** hands off awaiting-merge: DEV-463 (#n); only the user merges (Q7)

---

## Dependencies

- T000 before T001; T001 before any T002–T027.
- Phase 2 (T002–T005) before Phase 3 selection that persists keys; T003 before T004 persistence mapping.
- Phase 3 (T006–T009) before relying on enrichment to populate keys for API/UI proofs.
- T010 before T011 (DTO before details JSON proofs); T010 before T012–T013 (DTO before OpenAPI/generate).
- T013 before T014–T016 (generated types before typed UI/fixtures).
- T017 after T016 (and after C# stories green).
- T018–T023 after T017.
- T024 after T018–T023 code/web gates.
- T025 after T024 attempt (runtime `Could not run` still allows review to proceed but clearance stays blocked until dispositioned per Q5/Q7).
- T026–T027 after review clearance path.

## Sequencing examples

```text
# Phase 0 / 1
T000 Keel Gate 1 merge receipt
T001 Wisp recon (incl. unspecified copy sites) + Keel drift reanalysis + Gauge baseline

# Phase 2–3 backend (Anvil)
T002–T005 domain + EF Trailer key
T006–T009 TMDb selection + chain merge

# Phase 4–5 contract + UI (Anvil)
T010–T013 DTO → details JSON null/present → OpenAPI → generate:api
T014–T016 Trailer UI + MSW

# Phase 6–10
T017 Cog /refactor
T018–T023 Gauge gates
T024 runtime + CSP note + glossary read-back (landed T003)
T025 three-axis review
T026–T027 rebase / awaiting-merge → user merges
```

## Parallel opportunities

- After T001: T002 domain tests may proceed in parallel with early T006 test scaffolding only when file sets are disjoint; same-file adapter/persistence work stays sequential.
- T014 web tests may be authored after T013 in parallel with residual backend green-fix only when no shared files.

## MVP

MVP for Gate 1 is Phase A documents only. Build MVP after Gate 1 is T000–T016 (local Phases 0–5: Trailer key through UI). Full ticket delivery also requires T017–T027 (refactor, gates, runtime/CSP, review, delivery).

## Notes

- `[P]` only where different files and no incomplete deps; same-file edits stay sequential.
- Local `## Phase N:` IDs are for `/implement Phase N` dispatch and seat routing; they are not task-pipeline phase numbers.
- Do not edit files outside the Q8 bounded plan without a cited decision amendment. Missing facts → `needs recon` / `needs decision`, not invention.
- Out-of-scope Critical/High or broken behavior → Rigger follow-up ticket; otherwise noted, no ticket (Q6).
