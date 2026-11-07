# Implementation Plan: Browser playback default with Media Chrome

**Branch**: `feature/455-spec` | **Date**: 2026-11-07 | **Spec**: `specs/DEV-455/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q12); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-455`, `recon-DEV-455`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (gates cite only; Quill does not edit constitution). Dependencies: DEV-456, DEV-458.

**Status**: Draft pending ordinary owner Gate 1. Plan-challenge amendment pass applied from brief decision register / CONCLUSIONS adjudication (commit 94c06b57); freeze remains pending Keel's independent reread. No implementation clearance. No open Quill `needs decision`. Palette owns `design.md` amendments separately. Quill does not invent freeze.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md`, or `contracts/` — behaviour is ruled in the brief; public capability contract is specified in spec/plan/tasks generation steps. Quill does not invent stream/launch types absent at recon base.

## Summary

Make both existing Play entry points default to in-page browser playback through the DEV-456 stream, using Media Chrome for accessible controls and truthful media errors; expose a narrow LocalPlay capability query/handler and boolean-only endpoint so Open in BS Player appears only when effective LocalPlay permits it; keep browser Play independent of `POST /play`; verify real playback through Compose web nginx with a non-tracked browser-compatible sample while preserving ADR 0023 LocalPlay=false for Compose/e2e.

## Technical Context

**Language/Version**: C# 14 / .NET 10 backend; TypeScript + React 19 + Vite in `web/` (`TreatWarningsAsErrors=true`, nullable enabled, CPM).

**Primary Dependencies**: Existing pins plus **one new npm dependency**: Media Chrome (ticket-named; Q1). Pin policy is a deliberate ticket-specific adoption of Playwright-scoped ADR 0023 precedent (not a pre-existing general npm policy): highest stable Node-24-compatible release published at least 14 days before selection, exact version with no caret, Phase B registry evidence recorded; T005 also records `npm audit --omit=dev` outcome/advisory disposition or Could not run (Dependabot/NuGet gate do not establish that result; no gate-script changes) — **never invent a version in this plan**. No second player library, no second wrapper dependency; no new NuGet packages; no new state-management library.

**Storage**: N/A for new schema. Effective `FeatureOptions.LocalPlay` remains bound in ServiceDefaults; Api endpoint resolves `IOptions<FeatureOptions>` outside Core, constructs `GetPlaybackCapabilitiesQuery(bool LocalPlayEnabled)`, and maps Core `PlaybackCapabilitiesResult(bool LocalPlayEnabled)` to Api `PlaybackCapabilitiesDto` (C2; Ledger M2). Stream bytes owned by DEV-456. No playback-history writes from this ticket.

**Testing**: Vitest + React Testing Library for web; existing MSW/openapi-msw patterns; Playwright/e2e via existing `web/e2e`; xUnit v3 / Shouldly / existing API integration harness for capability endpoint. Prefer failing tests before production edits where feasible. Do not mock query/serialization behaviour that depends on real ASP.NET JSON shaping for the capability DTO. Real-host effective options on/off + serialized API JSON are the binding proof for capability mapping; handler bool unit tests are supplementary (Ledger L3). jsdom cannot prove codec decoding, resumed seeking, desktop fullscreen, or nginx range delivery — those require real-browser Compose receipts (Q7, Q10).

**Target Platform**: Docker Compose local stack (Api + `docker/nginx.conf` web proxy); desktop browsers for Media Chrome / fullscreen acceptance. Responsive mobile widths promise layout only — no mobile-specific fullscreen fallback (Sentry L4). Compose/e2e keep `Features.LocalPlay=false` (ADR 0023). Enabled local launch only on separate DEV-458-approved loopback deployment.

**Project Type**: Ports-and-adapters API with feature-organised Core, explicit handlers + decorators, and SPA feature folders (constitution I–II — not VSA). New UI under `web/src/features/playback/`. Capability use case is an explicit `IQueryHandler` in existing `Core/Features/Playback` registered via `AddHandler` (Tracing → Logging → Validation → handler) on the existing registration surface T001 identifies; thin API mapping stays in existing `LamuFlix.Api` Endpoints (extend observed `PlaybackEndpoints` or additive sibling under `Endpoints/` — Phase B recon confirms exact registration drift). Unobserved `HandlerRegistration` / `ApiEndpoints` paths are *(proposed, verified at T001)* (Compass F3). No new project, top-level layer, or Core port (Q9).

**Performance Goals**: Personal-library interactive playback; single active media element; teardown must stop audio promptly on navigation.

**Constraints**: Charter §2.3 items 1/4/5 deliberately ruled (Media Chrome; capability API; LocalPlay). Contract chain C# → OpenAPI → generated TS; never hand-edit generated artifacts. Browser Play never POSTs `/play`. No Process.Start/config/bind/schema changes. Preserve DEV-458. No fake DEV-456 types. Never lower harness thresholds. No explanatory comments outside allowed exemptions.

**Scale/Scope**: Size M; UI + narrow capability endpoint; parent estimate 2d.

## Constitution Check

Gates checked against `.specify/memory/constitution.md` (worktree current). Brief Q12: no ticket contradiction, no constitution departure, no owner structural checkbox, no new ADR.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; new port needs ADR; Core reference allowlist | **PASS** — new capability code introduces no `IOptions` / Microsoft.Extensions.Options in Core (constitution:288–290). Api endpoint resolves effective `FeatureOptions.LocalPlay` outside Core, constructs `GetPlaybackCapabilitiesQuery(bool LocalPlayEnabled)`, and maps Core `PlaybackCapabilitiesResult` to Api DTO — no constructor-bool/composition-boundary alternative, no Core-to-Api reference (C2; Ledger M1/M2). Pre-existing Core Options-reference drift (six handlers + AI/DI abstractions per recon-DEV-455:98–108) is acknowledged and left out of scope; follow-up noted, no automatic ticket or allowlist widening (Ledger M1). No new Core port, layer, ADR, or package (Q9, Q12). Stream consumption is web URL to DEV-456 route; launcher ports unchanged (Q2). |
| II (Explicit Handlers / Decorators) | Every use case is an explicit command/query handler with AddHandler + Tracing → Logging → Validation | **PASS** — local launch keeps existing `PlayMovie` command handler; new capability use case remains sealed `GetPlaybackCapabilitiesQuery(bool LocalPlayEnabled)` + sealed `GetPlaybackCapabilitiesQueryHandler` (`IQueryHandler`) returning Core `PlaybackCapabilitiesResult(bool LocalPlayEnabled)`, registered via `AddHandler` with Tracing → Logging → Validation on the existing surface T001 identifies; endpoint owns options resolution and DTO mapping (Q2–Q3; C2; Ledger M2; constitution:315–321). |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter/sort change. |
| IV (Domain transitions) | Movie mutators | **PASS** — no Movie mutation; no history writes (Q6). |
| V (Validated inputs / errors) | Stable public statuses | **PASS** — new capability route is 200 boolean-only; launch statuses preserved exactly as generated contract/current UI (Q2–Q3). |
| VI (Observability) | Safe diagnostics | **PASS** — no new sensitive config exposure; capability DTO forbids paths/player config (Q2); terminal media diagnostics record media error code / network-state via the existing pattern only — never URLs, paths, query strings, or configuration; no new telemetry dependency (Sentry L2). |
| VII (Configuration / LocalPlay / Process.Start) | LocalPlay gated; Process.Start careful | **PASS with deliberate ruling** — Q2/Q3/Q5 Patron approved capability surface and LocalPlay-gated secondary action; no Process.Start/config/bind changes; Compose LocalPlay remains false (Q7; ADR 0023). |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS with Q12 bound** — spec-local vocabulary clarifies existing Q2/Q12 meanings only; no glossary, CONTEXT.md, or ADR expansion (Compass F4). |
| IX (Test Pyramid) | Meaningful layers | **PASS** — Real-host effective options on/off + serialized API JSON are binding proof; handler bool mapping unit tests are supplementary; Vitest/RTL for UI/capability wiring including Space single-toggle, seek-ignore-until-bounds, cross-instance teardown ordering, and deterministic local recovery; real-browser Compose for codec/seek/desktop-fullscreen/range; no driver mocks for serialization-dependent capability JSON (Q10; C2; Ledger L3; Sentry M3/M4/L1/L4/L5). |
| Tech stack / API docs | OpenAPI chain | **PASS** — regenerate OpenAPI/TS/MSW via existing scripts; never hand-edit (Q2, Q8). |
| Governance / Static-Analysis Gates | Honest exits | Phase B / Build: report PASS/failure/SKIPPED/SKIP/N/A/Could not run honestly; web gates when `/web` changes; backend gates for actual C# diff; Size M three-axis review; two remediation rounds (Q11). |

### Complexity Tracking

No unjustified constitution violation. Complexity Tracking empty. Media Chrome + capability API + LocalPlay secondary action are Patron-cited Q1/Q2/Q3 rulings, not silent assumptions.

## Approach

- **Phase A (this draft)**: Quill writes spec/plan/tasks/checklist/receipt from brief/CONCLUSIONS/ASSUMPTIONS/recon facts. Keel runs read-only `/speckit-analyze`. Palette supplies `design.md` before plan challenge. No Build code/gates here; do not invent Media Chrome version or stream types.
- **Phase B pickup (after Gate 1 + DEV-456/458 delivered)**: Wisp fresh recon against landed main for generated stream/launch/OpenAPI types, playback endpoints, web Play entry points, `AddHandler` registration surface, and drift. T001 also records media-element credential compatibility and stream URL lifetime/reusability as authorized future-delivery pickup prerequisites (not invented contract facts). Keel reanalysis on drift. Gauge baseline. **STOP** on unresolved contract drift, or on header-only / expiring / single-use delivery incompatible with Q6 same-source Retry — new ruling required; signed URLs, auth redesign, and re-resolution are not preauthorized; never silent adaptation (Q8; Sentry M1/M2).
- **Dependency-ordered implementation (brief Q10)**:
  1. Pickup/dependency/Gate 1 recon (including T001 credential/URL-lifetime STOP checks and registration-surface identification).
  2. Capability query/handler: endpoint resolves `IOptions<FeatureOptions>` outside Core, constructs `GetPlaybackCapabilitiesQuery(bool LocalPlayEnabled)`, invokes decorated handler returning Core `PlaybackCapabilitiesResult(bool LocalPlayEnabled)`, maps to Api `PlaybackCapabilitiesDto`; `AddHandler` on the existing surface T001 identifies; real-host options on/off + serialized API JSON as binding proof; handler bool unit tests supplementary (C2; Ledger M2/L3).
  3. Regenerate OpenAPI → TypeScript (+ MSW handlers as repo scripts require).
  4. Add exact Media Chrome dependency (ADR 0023 ticket-specific pin + `npm audit --omit=dev` record or Could not run) via `web/package.json` + lockfile; no second wrapper.
  5. Accessible single player under `web/src/features/playback` with lifecycle/errors/controls/keyboard scoping (Space single-toggle; seek ignore-until-bounds; outgoing pause/clear before incoming attach; safe diagnostics; desktop fullscreen; deterministic local recovery).
  6. Wire MovieDetailsView + MovieCard + app-navigation player intent; always-offer Open in BS Player when capability true (hidden otherwise; never auto-launch); browser Play never POSTs `/play`; preserve launch contract regressions for the secondary action; cross-instance teardown tests on movie/card swaps.
  7. Vitest/RTL regression suites green.
  8. Real-browser Compose evidence last (provision git-ignored/outside-tree sample; scan/import or reuse already-scanned title; desktop fullscreen acceptance; record receipt or Could not run).
- **Design**: Palette owns `design.md` amendments (sections 7–9 taste / Compass F1–F2); Quill respects behaviour Q4–Q6/Q12 and does not edit design.
- **Refactor / ship**: Per task-pipeline after Build; web gates + applicable C# gates; Size M three-axis review under Q11 bar.

## Project Structure

Surfaces grounded in `brief.md` Planned file ownership and `recon-DEV-455` §§3–5, §7. Proposed paths stay inside existing folders and are marked *(proposed)*. Exact Media Chrome version and any unobserved helper filenames come from Phase B pickup — do not invent.

```text
specs/DEV-455/
  brief.md, CONCLUSIONS.md, ASSUMPTIONS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  checklists/requirements.md
  design.md                                 (Palette-owned; required before plan challenge)

src/LamuFlix.Core/Features/Playback/
  PlayMovieCommand.cs / PlayMovieCommandHandler.cs   (existing local launch)
  GetPlaybackCapabilitiesQuery.cs                    *(proposed)* sealed query record GetPlaybackCapabilitiesQuery(bool LocalPlayEnabled) (C2; Ledger M2)
  GetPlaybackCapabilitiesQueryHandler.cs             *(proposed)* sealed IQueryHandler → PlaybackCapabilitiesResult(bool LocalPlayEnabled); Core-only BCL/allowed refs — no IOptions
  PlaybackCapabilitiesResult.cs                      *(proposed)* sealed Core result record (not the Api DTO)

src/LamuFlix.Api/
  HandlerRegistration.cs                    *(proposed, verified at T001)* AddHandler for the new query on the existing registration surface T001 identifies; decorator order unchanged
  Endpoints/PlaybackEndpoints.cs            (existing play POST; add capabilities GET mapping or sibling under Endpoints/; resolve IOptions<FeatureOptions> here, construct query, map Core result → DTO)
  Endpoints/PlaybackCapabilitiesDto.cs      *(proposed)* boolean-only API DTO beside other endpoint DTOs — no Core-to-Api reference
  Endpoints/ApiEndpoints.cs                 *(proposed, verified at T001)* registration already maps playback if that is the surface T001 identifies

src/LamuFlix.Core/Options/FeatureOptions.cs (existing LocalPlay type; IOptions binding/resolution stays outside Core — ServiceDefaults bind + Api endpoint; no schema change; pre-existing Core Options drift elsewhere out of scope)

tests/LamuFlix.IntegrationTests/
  PlaybackEndpointTests.cs                  (extend or focused capability facts; recon §7)

web/
  package.json / package-lock.json          (Media Chrome exact ADR-0023 ticket-specific pin at pickup; npm audit --omit=dev recorded)
  src/api/openapi.json / types.ts           (generated only)
  src/features/playback/                    *(proposed)* player components, hooks, tests
  src/features/movie-details/MovieDetailsView.tsx  (Play → browser player; secondary BS Player)
  src/features/movie-details/movieDetailsApi.ts    (retain usePlayMovie for secondary only; capability client)
  src/library/MovieCard.tsx                 (Play → navigate + player intent)
  src/features/app-navigation/useAppNavigation.ts  (player intent transport only)
  e2e/                                      (existing harness; playback evidence / smoke support)
```

**Layout decisions**:

- No research/data-model/contracts folders — decisions frozen in brief Q1–Q12.
- Quill owns spec/plan/tasks/receipt/checklist. Quill does not edit brief/CONCLUSIONS/ASSUMPTIONS/constitution/production code/design.md.
- Capability is DEV-455-owned; stream bytes/range/resolver remain DEV-456; launcher hardening remains DEV-458.
- Browser media element uses the delivered DEV-456 stream URL as its source under gesture (product player UI is this ticket; DEV-456 itself ships no product player).

## Test Strategy

| Family | Required evidence |
|---|---|
| Capability query/API | Real-host effective options on/off + serialized API JSON as binding proof; handler unit facts for `GetPlaybackCapabilitiesQuery(bool)` → `PlaybackCapabilitiesResult(bool)` supplementary (no IOptions in Core tests); no paths/player config; no driver mocks for serialization-dependent JSON (C2; Ledger L3) |
| Generated contract | OpenAPI + `types.ts` (+ MSW) regenerated; web consumes generated types only |
| Capability UI | true/false/loading/failure; secondary always offered when true / hidden otherwise; browser Play still available on load/fail; terminal recovery never auto-launches (Sentry L5) |
| Browser vs launch | Browser Play never POSTs `/play`; only explicit capability-true action does; launch 204/403/404/409/422/other/network regressions + in-flight guard |
| Player lifecycle | native states; autoplay denial vs terminal failure; Retry same-source restart; safe media-error/network-state diagnostics (no URL/path/query/config); no auto-retry/fallback; single active source; outgoing pause/source-clear before incoming attach on movie/card swaps; stale async ignored (Sentry M3/L2) |
| Controls / a11y | pointer + keyboard actions; Space on focused control = one toggle / scroll prevented + region-focus parity; seek ignore until valid bounds then clamp; speed; desktop fullscreen (layout-only on mobile widths); labelled Tab/visible focus; shortcuts excluded from editable/dialog focus (Sentry M4/L1/L4) |
| Media Chrome supply | exact ADR-0023 ticket-specific pin + registry evidence; `npm audit --omit=dev` outcome/advisory disposition or Could not run; no gate-script edits (Ledger M3/L2) |
| Compose / real browser | provisioned sample + scan/import or already-scanned title; controls; resumed seeking/range through nginx; desktop fullscreen; errors; LocalPlay-off hidden secondary; recorded SHA evidence or Could not run (Sentry L3/L4) |
| Pickup STOP checks | T001 media-element credential compatibility + URL lifetime/reuse; STOP on header-only or expiring/single-use incompatible with same-source Retry; registration surface identified (Sentry M1/M2; Compass F3) |
| Gates | `scripts/run-web-gates.ps1`; every applicable backend gate for actual C# diff; honest non-pass labels; no new gate scripts |

## Gates (after Gate 1 and implementation — Gauge owns configured Phase 3 gates)

Gates run **sequentially** (shared worktree outputs; no parallel gate scripts).

1. Web: `scripts/run-web-gates.ps1` when `/web` changed.
2. Changed-C# scope (if any): Roslyn, cyclomatic (+ refactor threshold from `harness.yml`), InspectCode.
3. Affected suites then full `dotnet test` when C# changed; Vitest via web gates.
4. Property / vulnerable-packages / format per harness applicability; honest SKIPPED/SKIP/N/A/Could not run.
5. Mutation when eligible production C# under `src/` changed; otherwise script SKIPPED (scope-empty) / N/A as applicable — never manufacture PASS.
6. Size M three-axis review (Sentry/Ledger/Compass) under Q11 closing bar; max two remediation rounds; unresolved blockers stop for Patron/Conductor ruling.
7. Rebased gates before awaiting-merge; user merges.

Phase A runs none of these and claims no gate result.

## Risks

- **Dependency not landed**: DEV-456/458 still Spec at recon base — mitigated by Q8 Phase A/B split; pickup recon; stop on drift.
- **Autoplay / codec variance**: Mitigated by recoverable explicit Play, generic terminal errors, and real-browser Compose receipt (not jsdom).
- **LocalPlay exposure**: Mitigated by boolean-only capability DTO; server still enforces launch; Compose stays LocalPlay=false.
- **Keyboard conflicts**: Mitigated by player-owned focus scoping and existing Escape/dialog patterns (recon §3).
- **Sample media dirtying git**: Mitigated by git-ignored/outside-tree `LIBRARY_PATH` policy (Q7).
- **Media Chrome pin / audit**: Exact ADR-0023 ticket-specific pin + `npm audit --omit=dev` at T005; not invented here; no gate-script changes (Q1; Ledger M3/L2).
- **Stream credential / URL lifetime**: T001 pickup STOP checks; no signed-URL/auth redesign/re-resolution preauthorized (Sentry M1/M2).
- **design.md amendments**: Palette-owned (Compass F1/F2 / Ledger L1); Quill does not edit design.

## Ordering

Matches brief Q10: recon → capability → OpenAPI/types → Media Chrome pin → player → detail/card/LocalPlay integration → component regressions → Compose evidence. US1 before US4 secondary visibility; US2 before/with US3 wiring; US5 last.

## ADR / glossary status

No new ADR and no CONTEXT.md glossary rewrite (Q12). Existing LocalPlay / browser playback / capability meanings suffice.
