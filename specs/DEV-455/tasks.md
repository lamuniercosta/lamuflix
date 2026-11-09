# Tasks: Browser playback default with Media Chrome

**Input**: `specs/DEV-455/` spec.md, plan.md, grill-closed brief.md (Q1–Q12), CONCLUSIONS.md, ASSUMPTIONS.md, recon-DEV-455, note DEV-455, specs/PRODUCT.md

**Status**: PLAN FROZEN; Gate 1 satisfied by spec PR #340 merged at `1955de8a`. T001 complete at pickup HEAD `c369465fb32d6e3d1d9ed25fec752881a4e6c68d`: dependency delivery, stream URL reuse, native media-element compatibility in the inspected local/Compose deployment, registration surface and Gauge baseline are recorded in CONCLUSIONS.md. Phase 2 T002+ is unblocked in the approved dependency order. Baseline analyzers are SKIPPED (scope-empty), not PASS. No implementation, runtime acceptance or merge clearance is claimed. Historical statuses are superseded by the T001 completion receipt.

**Gate 1**: No owner structural checkbox (brief Q12; PRODUCT.md §5). Handoff: **Patron provisional Gate 1 → Rigger opens the spec PR → user merges Gate 1 → Keel T000** records the verified merge receipt in `specs/DEV-455/CONCLUSIONS.md`.

**Tests**: Required (brief Q10; FR-019 family). Prefer new-behaviour regressions red before production edits where feasible. Never hand-edit generated OpenAPI/TS/MSW. Never invent Media Chrome version or DEV-456 stream types. Never lower harness thresholds.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared build outputs (gates) are never `[P]`.

**Conventions**: Web tests Vitest/RTL; backend `Method_Condition_Expected`; only `// arrange` / `// act` / `// assert` in C# tests; no explanatory code comments; TimeProvider when time is needed; literals used in static contexts are `const`.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement Phase N` / `web-implement` dispatch. They are **not** canonical `task-pipeline` phases.

---

## Local phase dependency / execution order

```text
Phase 0   T000 (Keel Gate 1 record) — blocks T001+
Phase 1   T001 (Wisp drift recon; Keel reanalysis; Gauge baseline; deps delivered) — blocks T002+
Phase 2   T002 → T003 → T004   (Anvil; US1 capability query/handler + DTO/endpoint + OpenAPI/types)
Phase 3   T005 → T006 → T007 → T008   (Anvil / web-implement; US2 Media Chrome pin + player)
Phase 4   T009 → T012   (web-implement; US3 details/card browser Play wiring)
Phase 5   T013 → T015   (web-implement; US4 Open in BS Player + launch regressions)
Phase 6   T016 → T017   (Anvil/web; US5 Compose sample + real-browser evidence)
Phase 7   T018          (Cog /refactor once if C# changed; web cleanup in-scope)
Phase 8   T019 → T022   (Gauge gates — sequential)
Phase 9   T023          (canonical Phase 4 Size M review)
Phase 10  T024          (canonical Phase 5 delivery)
```

---

## Phase 0: Gate 1 record (blocks Phase 1)

**Purpose**: Confirm user Gate 1 merge before any Build work.
**Checkpoint**: Verified merge receipt in CONCLUSIONS.md; or stop.

- [X] T000 **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-455 in `specs/DEV-455/CONCLUSIONS.md` — receipt only. T001–T024 MUST NOT proceed until this receipt exists (brief Q8/Q12; FR-020).

---

## Phase 1: Pickup — dependency delivery, fresh recon, drift reanalysis (blocks implementation)

**Purpose**: Confirm Gate-1-merged artifacts still match landed main; confirm DEV-456 stream and DEV-458 launch contracts are delivered and present in generated OpenAPI/TS; confirm playback/web entry-point drift only.
**Checkpoint**: Fresh recon recorded; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift; unresolved drift/decision stops — no invention. Baseline captured.

- [X] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main for recon-bounded surfaces (`PlaybackEndpoints`, FeatureOptions/LocalPlay binding, DEV-456 stream route + generated `web/src/api/openapi.json` / `types.ts` stream path, DEV-458 launch behaviour, `MovieDetailsView` / `MovieCard` / `usePlayMovie` / `useAppNavigation`, `web/package.json` pin policy precedents, `web/e2e` harness, Compose nginx/`LIBRARY_PATH` sample policy, ADR 0023 LocalPlay=false). Explicit authorized future-delivery prerequisites (not invented contract facts): (a) record how a media element authenticates the delivered stream URL (same-origin cookie / credential mode usable from `<video>` / Media Chrome — not a header-only contract the element cannot send); (b) record stream URL lifetime/reusability for Q6 same-source Retry; (c) identify the existing `AddHandler` registration surface (unobserved `HandlerRegistration.cs` / `ApiEndpoints.cs` paths are proposed until verified here). Confirm DEV-456 and DEV-458 are delivered before player consumption tasks. **Keel** runs cross-artifact reanalysis of brief/spec/plan/tasks against landed main. Amend tasks only where authorized by demonstrated drift. **STOP** on missing stream/launch contract, unresolved drift, or header-only / expiring / single-use delivery incompatible with Q6 same-source Retry (new ruling — no fake types; signed URLs, auth redesign, and re-resolution are not preauthorized). **Gauge** records pre-edit baseline. No production edits in this task (brief Q8; FR-020; Sentry M1/M2; Compass F3).

---

**T001 completion receipt — 2026-11-09, pickup HEAD `c369465fb32d6e3d1d9ed25fec752881a4e6c68d`:** Keel completed bounded manual cross-artifact reanalysis and closed the two remaining evidence requirements using `recon-DEV-455:3-8` and note `DEV-455:54-66`. API startup, route grouping and Compose nginx require no HTTP authentication; native video needs no custom Authorization header in the inspected deployment. Gauge captured the pre-edit Roslyn, cyclomatic and InspectCode baseline: each exit 2, **SKIPPED (scope-empty)**, 0 findings, nothing verified. Previously recorded DEV-456/458 delivery, same-source Retry and registration checks remain satisfied. T001 is complete; Phase 2 starts at T002 and subsequent tasks follow the frozen order. Unknown user-specific external proxy authentication is outside the inspected configuration, not a claimed compatibility result. Scripted SpecKit analysis remains **Could not run** (branch-name guard); no scripted PASS, production-gate PASS or runtime acceptance is inferred. See CONCLUSIONS.md, T001 completion receipt, for provenance and dispositions.

## Phase 2: Playback capabilities contract (Anvil; Priority: P1 — User Story 1)

**Goal**: Explicit capability query/handler returns effective LocalPlay; API maps to boolean-only DTO; regenerate OpenAPI/TS.
**Independent Test**: Real-host effective options on/off + serialized API JSON binding proof green; handler bool mapping unit tests supplementary; generated types expose `localPlayEnabled` only; no sensitive fields.
**Order**: T002 → T003 → T004 (acyclic; T004 makes T002 green after T003).

- [ ] T002 [US1] After T001: Add failing unit coverage for `GetPlaybackCapabilitiesQueryHandler` mapping `GetPlaybackCapabilitiesQuery(bool LocalPlayEnabled)` → `PlaybackCapabilitiesResult(bool LocalPlayEnabled)` true/false (no `IOptions` in Core tests; supplementary) under `tests/` (Playback feature unit project pattern used by `PlayMovieCommandHandlerTests`) and failing API/integration coverage for `GET /api/playback/capabilities` proving real-host effective options on/off resolve outside Core into 200 `application/json` with only `localPlayEnabled` and absence of paths/player configuration (binding proof), under `tests/LamuFlix.IntegrationTests/` (extend `PlaybackEndpointTests.cs` or focused sibling named by T001) (FR-012; Q2; SC-003; C2; Ledger M2/L3)
- [ ] T003 [US1] After T002: Implement sealed `GetPlaybackCapabilitiesQuery(bool LocalPlayEnabled)` + sealed `PlaybackCapabilitiesResult(bool LocalPlayEnabled)` + sealed `GetPlaybackCapabilitiesQueryHandler` (`IQueryHandler`) under `src/LamuFlix.Core/Features/Playback/` — Core must not reference `IOptions` / Microsoft.Extensions.Options and must not reference Api DTOs (no new Core port, layer, ADR, package, or Options dependency from this code; pre-existing Core Options drift out of scope; C2; constitution I; Ledger M1/M2); register via `services.AddHandler<...>` on the existing registration surface T001 identifies (`src/LamuFlix.Api/HandlerRegistration.cs` or equivalent) with Tracing → Logging → Validation → handler; in `src/LamuFlix.Api/Endpoints/PlaybackEndpoints.cs` (or additive Endpoints sibling if T001 shows a cleaner existing pattern) resolve `IOptions<FeatureOptions>` outside Core, construct `GetPlaybackCapabilitiesQuery(bool LocalPlayEnabled)`, invoke the decorated handler, and map Core `PlaybackCapabilitiesResult` to Api `PlaybackCapabilitiesDto` (only `bool LocalPlayEnabled`) for `GET /playback/capabilities` with OpenAPI Produces metadata — no constructor-bool/composition-boundary alternative; no schema/options shape change beyond the new route/DTO (FR-012, FR-017; Q2; constitution I–II; C2; Compass F3)
- [ ] T004 [US1] After T003: Make T002 green; regenerate contract chain with existing tools (`LAMUFLIX_REGENERATE_OPENAPI` / `npm run generate:api` / `npm run check:api` as repo scripts require) updating `web/src/api/openapi.json`, `web/src/api/types.ts`, and MSW handlers — never hand-edit generated artifacts (FR-013; Q2, Q8)

---

## Phase 3: Media Chrome player (web-implement; Priority: P1 — User Story 2)

**Goal**: Exact Media Chrome pin; cohesive accessible player with lifecycle/errors/controls under `web/src/features/playback`.
**Independent Test**: Vitest/RTL player suites cover states, controls, focus scoping, seek clamp, retry/teardown, autoplay denial vs terminal failure without card navigation.
**Order**: T005 → T006 → T007 → T008. **Prerequisite**: T001 confirmed stream URL/contract available for source wiring (or player accepts injected source URL for unit tests until integration).

- [ ] T005 [US2] After T004: Select and verify `media-chrome` as the highest stable Node-24-compatible release published at least 14 days before selection (ticket-specific ADR 0023 precedent, not general npm policy); exact pin with no caret; record Phase B registry evidence; add it only to `web/package.json` and update `web/package-lock.json`; no second player/state library or second wrapper dependency; record `npm audit --omit=dev` outcome and advisory disposition, or Could not run — Dependabot/NuGet gate do not establish this result; no gate-script changes (FR-003; Q1; Ledger M3/L2)
- [ ] T006 [US2] After T005: Add failing Vitest/RTL coverage under `web/src/features/playback/` for native media states, recoverable autoplay denial vs terminal failure, Retry restart-from-beginning, no auto-retry/local fallback, deterministic terminal recovery (always offer explicit local action when capability true / hide otherwise / never auto-launch), single active source, teardown ordering (outgoing pause + source-clear completes before incoming source attaches; stale-work rejected on movie replacement), seek ignore until valid seekable bounds then clamp (never throw/corrupt timeline), speed 0.5/1/1.5/2, desktop fullscreen control presence (no mobile-specific fullscreen fallback asserted), labelled Tab/visible focus, and keyboard Space/K/arrows/M/F/Escape scoped to player-owned focus only including Space on a focused control as exactly one toggle with scroll prevented and equivalent region-focus behaviour (exclude editable/dialog focus) (FR-004–FR-010; Q5–Q6; SC-004–SC-006; Sentry M3/M4/L1/L4/L5)
- [ ] T007 [US2] After T006: Implement cohesive Media Chrome player components/hooks in `web/src/features/playback/` that request the delivered DEV-456 stream URL under user gesture, apply FR-004–FR-010 behaviours (including Space single-toggle parity, seek ignore-until-bounds, outgoing-before-incoming teardown, desktop fullscreen scope, deterministic local recovery), record safe media error code/network-state diagnostics via the existing pattern only (never URLs/paths/query/config; no new telemetry dependency), use generated types only where HTTP client types apply, and expose a labelled region suitable for details embedding per Palette `design.md` (behaviour remains brief Q4–Q6; closed/idle is initial only — no close control) (FR-002–FR-011, FR-017; Q4–Q6, Q9; Sentry L2; Compass F1)
- [ ] T008 [US2] After T007: Make T006 green against T007 without requiring LocalPlay-on or card navigation (FR-004–FR-010)

---

## Phase 4: Details and card Play default to browser (web-implement; Priority: P1 — User Story 3)

**Goal**: Both Play entry points open the in-page browser player; browser Play never POSTs `/play`.
**Independent Test**: MovieDetailsView / MovieCard / navigation tests prove browser path and no launch POST from browser Play.
**Order**: T009 → T012.

- [ ] T009 [P] [US3] Add/extend failing coverage in `web/src/features/movie-details/MovieDetailsView.test.tsx` (and related) proving details Play opens the labelled player and does not call `POST /api/movies/{id}/play` (FR-001; Q3–Q4; recon-DEV-455:67)
- [ ] T010 [US3] Wire `web/src/features/movie-details/MovieDetailsView.tsx` so primary Play opens the `web/src/features/playback` player and starts stream under gesture; keep navigation usable; one active video; focus labelled region (FR-001, FR-002; Q4)
- [ ] T011 [US3] Extend `web/src/library/MovieCard.tsx` + `web/src/features/app-navigation/useAppNavigation.ts` (and tests `MovieCard.test.tsx` / `useAppNavigation.test.tsx`) so card Play navigates to details with player intent, opens the same player, attempts playback, and retains explicit Play if autoplay is blocked — without posting `/play` from that action; include cross-instance coverage that outgoing pause + source-clear completes before incoming source attaches and stale async work is rejected across the movie/card navigation swap (FR-001, FR-002, FR-010, FR-017; Q4; Sentry M3)
- [ ] T012 [US3] Make T009/T011 green; assert browser Play path never issues local-launch POST (FR-001; Q3)

---

## Phase 5: Open in BS Player (web-implement; Priority: P1 — User Story 4)

**Goal**: Capability-gated secondary action; launch contract regressions preserved; no automatic fallback.
**Independent Test**: Capability true/false/loading/failure visibility; launch POST only from secondary action; existing status mappings.
**Order**: T013 → T015.

- [ ] T013 [P] [US4] Add failing Vitest/RTL coverage for capability true/false/loading/failure secondary visibility; Open in BS Player invokes existing `usePlayMovie` / `POST /api/movies/{id}/play` only when capability is true; preserve current UI meanings for 204/403/404/409/422/other/network and in-flight guard; terminal browser error always offers the explicit local recovery action when capability is true, hides it otherwise, and never auto-invokes it (FR-014–FR-016; Q2–Q3; recon-DEV-455:67–69; Sentry L5)
- [ ] T014 [US4] Integrate capability query + Open in BS Player into `MovieDetailsView` / movie-details API module using generated types; visible copy Play / Open in BS Player / Retry with accessible labels per ASSUMPTIONS/Palette; always offer secondary when capability true / hide otherwise; do not claim every configured player is BS Player; do not weaken server enforcement (FR-014–FR-016; Q3, Q12; Sentry L5)
- [ ] T015 [US4] Make T013 green; confirm browser Play still never POSTs `/play` after secondary wiring; assert deterministic local-recovery visibility (always when capability true; never auto-launch) (FR-001, FR-015; Sentry L5)

---

## Phase 6: Compose real-browser evidence (Priority: P2 — User Story 5)

**Goal**: Provision non-tracked sample; record Compose/nginx real-browser receipt or Could not run; keep LocalPlay=false.
**Independent Test**: Runtime evidence file/note with required fields; or Could not run.
**Order**: T016 → T017.

- [ ] T016 [US5] Provision a small real browser-compatible sample via `LIBRARY_PATH` from a verified git-ignored runtime location or outside the tracked tree; scan/import that sample (or reuse an already-scanned title) so a playable movie exists before T017; record provenance/license; never replace tracked zero-byte `docker/sample-library` placeholders; never commit unlicensed media; keep Compose/e2e `Features.LocalPlay=false` (FR-018; Q7; ADR 0023; Sentry L3)
- [ ] T017 [US5] Execute real-browser Compose verification through web nginx for SMOKE-001–003 family against a scanned/imported or already-scanned title (controls, resumed seeking/range evidence, desktop fullscreen acceptance — layout-only on mobile widths with no mobile-specific fullscreen fallback, errors, LocalPlay-off hidden Open in BS Player) and record browser/version, codec/container, duration, Compose URL, and exact status against the delivery SHA; if browser/sample unavailable report **Could not run** (never PASS); enabled-launch coexistence only on separate DEV-458-approved loopback — do not set Compose LocalPlay true (FR-019; Q7; SC-007; Sentry L3/L4)

---

## Phase 7: Refactor (Cog)

- [ ] T018 After T017: **Cog** runs `/refactor` once over completed implementation for this ticket (C# if changed; web complexity/duplication in scope of web-implement cleanup); fix without expanding scope or lowering thresholds (brief Q10–Q11).

---

## Phase 8: Verification gates (Gauge) — sequential

- [ ] T019 **Gauge** runs `scripts/run-web-gates.ps1` for DEV-455 `/web` changes (FR-019; plan Gates). Sequential with T020–T022 — not parallel.
- [ ] T020 **Gauge** runs applicable changed-C# Roslyn, cyclomatic (build then refactor threshold from `harness.yml`), and InspectCode scripts when `.cs` files changed; if no `.cs`, report `SKIPPED (scope-empty)` honestly — never PASS (FR-019). After T019.
- [ ] T021 **Gauge** runs affected suites / full `dotnet test` when C# changed; report web test results via web gates; honest SKIPPED/SKIP/N/A/Could not run (FR-019). After T020.
- [ ] T022 **Gauge** runs property / vulnerable-packages / `dotnet format --verify-no-changes` / mutation per harness applicability for this diff; mutation SKIPPED (scope-empty) or N/A when no eligible production C# — never manufacture PASS (FR-019; Q10). After T021; sequential.

---

## Phase 9: Size M review (canonical Phase 4)

- [ ] T023 **Sentry / Ledger / Compass** three-axis review under brief Q11 closing bar (zero unresolved in-scope Critical/High/Medium; Low documented/dispositioned; frozen Q1–Q10 scope; two review/fix rounds then stop for fresh Patron/Conductor ruling). Unresolved blockers return to Bernstein — do not silently extend or clear (Q11; FR-021).

---

## Phase 10: Delivery (canonical Phase 5)

- [ ] T024 Rebase onto latest default branch; re-run required gates on rebased HEAD; open/update PR with merge-bar proof; no CONTEXT.md glossary rewrite; no ADR accept task (Q12); user merges (PRODUCT §2)

---

## Dependencies & Execution Order

- Phase 0 blocks all Build.
- Phase 1 blocks production edits; requires DEV-456/458 delivered contracts.
- Phase 2 (US1) before Phase 5 secondary visibility that depends on generated capability types; may proceed before Media Chrome.
- Phase 3 (US2) before Phase 4 wiring that embeds the player.
- Phase 4 (US3) before/with Phase 5; browser Play must remain launch-free after secondary wiring (T015).
- Phase 6 after functional suites exist; real-browser evidence last.
- Phases 7–10 follow task-pipeline Build → refactor → sequential gates → review → ship.

### Parallel opportunities

- After T008: T009 (`MovieDetailsView.test.tsx` coverage) may run in parallel with T011's `MovieCard.test.tsx` / `useAppNavigation.test.tsx` scaffolding (different files). T010 stays after T009 on `MovieDetailsView.tsx`.
- After T004 and T008: T013 failing secondary/capability UI coverage may run in parallel with T014 production wiring when those files do not overlap (different files).
- **Not parallel / acyclic schedule**: T002 → T003 → T004 (T004 makes T002 green; never require T004 before T002). T005 → T006 → T007 → T008 (pin before player tests). T019–T022 (shared outputs). Same-file MovieDetailsView edits (T010/T014) sequential.

### MVP

US1 + US2 + US3 (capability, Media Chrome player, both Play entry points defaulting to browser) is the minimum operable browser-default slice; US4 is required for ticket secondary offer; US5 is required runtime acceptance.

## Notes

- `[P]` tasks = different files, no incomplete deps, no shared build outputs.
- Exact Media Chrome version is a pickup verification output of T005 under the ADR 0023 ticket-specific pin rule — Quill does not invent it here.
- Stream URL/types come from delivered DEV-456 via T001 — no handwritten substitutes. T001 credential/URL-lifetime checks are authorized future-delivery prerequisites, not invented contract facts.
- Local `tasks.md` Phase N ≠ canonical task-pipeline phase numbers.
- Palette owns `design.md` amendments (taste / close-affordance wording); Quill does not edit design.
- Plan-challenge dispositions are applied in this draft; none silently deferred past freeze. Gate 1 and runtime acceptance remain pending.
