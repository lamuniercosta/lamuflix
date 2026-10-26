# Implementation Plan: DEV-422 SignalR enrichment-status client hook

**Branch**: `feature/422-spec` | **Date**: 2026-10-26 | **Spec**: `specs/DEV-422/spec.md`

**Input**: Feature specification from `specs/DEV-422/spec.md`, frozen grill brief `specs/DEV-422/brief.md`, facts from note `recon-DEV-422`, Patron rulings in `specs/DEV-422/CONCLUSIONS.md`, taste receipt `specs/DEV-422/ASSUMPTIONS.md`, `specs/PRODUCT.md`.

**Status**: Draft planning artifact only (Phase A). Not frozen; Gate 1 not claimed and stays closed until the owner checkbox is answered. No gates or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1. Phase B remains STOP until owner checkbox resolution; landed approved upstream DEV-330 constitution disposition/amendment; landed hub implementation/generated-schema chain; and contract-drift verification against main (brief plan ordering step 1; Phase 2 Analyze Rulings).

## Summary

Deliver a frontend-only SignalR client for enrichment status: pin exact `@microsoft/signalr` 10.0.x, add `useEnrichmentStatus` with a narrow connection factory/owner seam, mount once under the root QueryClientProvider, update TanStack browse caches on `EnrichmentStatusChanged`, bridge one-way to nested details, invalidate on start/reconnect (including Pending), remove DEV-80 browse polling while preserving optimistic import/retry semantics, and prove behavior with a Vitest connection fake. Server hub remains DEV-330. Payload typing is conditional on the owner checkbox (generated `EnrichmentStatusChangedDto` recommended). Nothing else ships.

## Domain vocabulary

| Term | Definition source | Notes |
|------|-------------------|-------|
| enrichment status / Pending | brief Test strategy; recon types | Existing; no CONTEXT.md edit |
| enrichment-status event | brief Q2 | `EnrichmentStatusChanged` |
| reconnect/refetch | brief Q4; ADR-0022 | HTTP invalidate on start/reconnect |
| EnrichmentStatusChangedDto | brief Q2; DEV-330 FR-011 | Generated consumption subject to owner checkbox |

CONTEXT.md is not edited. Proposed ADR-0029 remains Proposed. Accepted ADR-0022 supplies transport context; no new ADR.

## Technical Context

**Language/Version**: TypeScript / React 19 SPA under `web/`; no C# production changes in this ticket.

**Primary Dependencies**: Add pinned exact published compatible `10.0.x` `@microsoft/signalr` via `--save-exact` to `web/package.json` and lockfile root (no caret/tilde ranges). Patch verified at Phase B pickup — do not invent a version in Phase A (brief Q6). Consume existing React, TanStack Query, Zod, Vitest, RTL already in `web/package.json`. No server NuGet package. No new validation library or connection-fake package.

**Storage**: N/A. No database, schema, or migration change (brief Q6).

**Testing**: Vitest + RTL with a controllable connection fake (not MSW hub simulation, not a real network connection). Named test paths:

- New: `web/src/library/useEnrichmentStatus.test.tsx`
- Repurpose in place: `web/src/library/libraryQueries.polling.test.tsx`
- Update in place: `web/src/library/LibraryView.retry.test.tsx`
- Focused App: `web/src/App.test.tsx` (existing; additive proofs for root mount / session connection)
- Focused details: `web/src/features/movie-details/MovieDetailsView.test.tsx` (existing; additive proofs for one-way bridge / recovery)
- Conditional e2e: `web/e2e/import.e2e.ts` only when required by changed refresh behavior and a real landed hub is available; otherwise `Could not run`, never PASS

Full Vitest connection-fake lifecycle/cache/race matrix remains mandatory (brief Test strategy). **propertyTests: opt-out — web-only SignalR connection lifecycle and TanStack Query cache orchestration; no new or changed domain invariants** (brief Phase 2 analyze decisions; CONCLUSIONS Phase 2 Analyze Rulings). Gauge records actual command/exit; an accepted opt-out is never PASS. Add no property-testing dependency. No Gherkin/Reqnroll. No new end-to-end framework or dependency.

**Target Platform**: Existing Vite/React SPA (`web/`). No ui tag; no Palette `design.md`.

**Project Type**: New hook file under existing `web/src/library/` plus bounded edits to shell, library queries, and movie-details files listed in brief Q6.

**Performance Goals**: No numeric latency target in the brief. Coalesce concurrent refresh work; no independent unbounded request loops. A valid event during an older in-flight refetch must still drive a fresh authoritative pass — coalescing must not drop that invalidation (brief Q4; Phase 2 plan-challenge item 1).

**Constraints**: Exact `@microsoft/signalr` pin only (approved dependency; `--save-exact`, no ranges at package/lock root). No new project, top-level folder, architectural layer/state library, context/provider/store/event-bus module, or module-global mutable registry. No backend route/status/DTO change (consume DEV-330 by reference subject to owner checkbox). No schema/migration. No `Features:LocalPlay`, secrets, or `Process.Start`. No hand-written DTO duplicates or hand-edited generated files. No uncontracted positive-only id rule. No visual Pending redesign. No rollback machinery for failed refetches. Thresholds stay in `harness.yml`. Browser runtime imports + `import type` for type-only symbols; no Node-only transport in browser runtime. Follow established conventions and mandatory web verification per brief Test strategy / `AGENTS.md`; post-pin `npm audit` records introduced-finding disposition without blanket-failing unrelated historical advisories; build size delta without new tooling/budget (Phase 2 plan-challenge items 2–4, 6).

**Scale/Scope**: Frozen scope sentence (brief Loop discipline): Q1–Q6 including the conditional generated-type choice and the enumerated production/test boundaries; anything else is a follow-up issue, not a finding in this round.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design. Every gate in this Constitution Check cites a numbered Core Principle (I–IX). Constraints that live only under unnumbered constitution sections are listed in **Unnumbered constitution authority checks** below, not here. Scope and Phase B prerequisites remain under **Scope and prerequisite checks**.*

- **Principle I** (ports and adapters, feature-organised Core; `.specify/memory/constitution.md:127-159`): No new port, generic repository, or UnitOfWork. Scope stays inside existing `web/src` library and movie-details folders (brief Q3, Q6; PRODUCT.md).
- **Principle II** (explicit handlers and decorators; `.specify/memory/constitution.md:161-178`): No MediatR. This frontend-only slice adds no handler/decorator registration surface (brief Q1, Q6).
- **Principle VII** (configuration isolation and deterministic time; `.specify/memory/constitution.md:275-294`): No secret, `Features:LocalPlay`, or `Process.Start` (care list item 5; brief Q6). Hub URL uses existing `VITE_API_BASE_URL || window.location.origin` with `withCredentials: false` (brief Q3).
- **Principle VIII** (ubiquitous language; `.specify/memory/constitution.md:296-311`): Retained terms only (enrichment status, Pending, enrichment-status event, reconnect/refetch); no new glossary entry; CONTEXT.md untouched; no new ADR (brief Test strategy; Q1).
- **Principle IX** (test pyramid; `.specify/memory/constitution.md:316-343`): Vitest + RTL + connection fake; no second assertion/mocking stack for the hub; generated contract type consumption subject to owner checkbox (brief Q2, Test strategy).

### Unnumbered constitution authority checks

Binding constraints from unnumbered constitution sections. **Numbered principle: none** for these items; do not invent I–IX mappings.

- **Technology Stack Constraints** (`.specify/memory/constitution.md:369`): Frontend stack is Vite + React + TypeScript strict, TanStack Query, Zod, `openapi-typescript` + `openapi-fetch`; Vitest + React Testing Library + MSW. This ticket adds pinned `@microsoft/signalr` per ticket/ADR-0022 (brief Q1, Q6).
- **API and Contract Rules** (`.specify/memory/constitution.md:504-507,534,545-565`): OpenAPI committed at `web/src/api/openapi.json`; TypeScript client and MSW handlers generated from that document, never hand-written. Hub payload consumption follows the generated schema path subject to the owner checkbox (brief Q2). Constitution governance remains authoritative for enrichment-status polling text and any departure. Accepted ADR-0022 accepts SignalR transport and notes a pending constitution amendment; it does not itself amend or supersede constitution polling text. Upstream DEV-330 still carries the open Gate 1 constitution-departure owner checkbox and Proposed ADR-0029 (`recon-DEV-422:130-132`; brief Phase 2 analyze decisions; CONCLUSIONS Phase 2 Analyze Rulings). No present constitutional-compliance clearance is claimed. Constitution-text editing stays outside DEV-422 scope (brief Q1); no duplicate constitution owner checkbox is introduced on DEV-422.
- **Coding Conventions — Comments** (`.specify/memory/constitution.md:452-462`): No explanatory comments beyond AAA test headers, empty-block justifications, and scoped warning suppressions.
- **Pull Request Quality Gates** (`.specify/memory/constitution.md:406-408`): PR checklist requires the same comment exemptions. Gate runs use configured scripts and harness thresholds only; never lower thresholds or invent a threshold value (brief Test strategy / `AGENTS.md`).

Post-design re-check: the numbered-principle Constitution Check gates above still hold. This plan adds no project, new port name, CONTEXT.md edit, ADR, or file outside the approved brief file set. The approved npm dependency is the ticket-named `@microsoft/signalr` pin only. Unnumbered constitution authority checks and scope/schema/migration constraints remain in force under their sections below; they are not waived by this re-check and are not Constitution Check gates. Side design files (`research.md`, `data-model.md`, `contracts/`, `quickstart.md`) are not produced by Quill — the brief already freezes contracts, files, ordering, and proofs.

## Scope and prerequisite checks

Binding product, care-list, and Phase B prerequisite constraints. **Numbered principle: none applicable** for each item; do not invent I/II/IX citations. Constraints remain in force.

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). Scope is frozen to brief Q1–Q6 / task-note `DEV-422:14-18`. Nothing is added or dropped beyond the frozen brief.
- Owner checkbox (ticket-text change): `blocked: structural — replace ticket-mandated hand-typed hub payload types with the generated DEV-330 schema type?` — Gate 1 stays closed until answered (brief Status; CONCLUSIONS Q2; PRODUCT.md §2.3(a)).
- No schema or migration (care list item 3; brief Q6).
- Public hub contract shape is consumed, not redefined: `/hubs/enrichment`, `EnrichmentStatusChanged`, `{ id, enrichmentStatus }` per brief Q2 and recon `102-126`. DEV-422 does not implement the server hub.
- Care list item 1: pinned `@microsoft/signalr` approved (brief Q1, Q6). Care list item 2: no new top-level folder/layer (brief Q3). Care list item 6: enumerated file set; no deletions (brief Q6).
- Epic / sequencing: Phase B STOP until owner checkbox resolved; landed approved upstream DEV-330 constitution disposition/amendment; hub implementation and generated OpenAPI/types landed; and drift check against main passes (brief plan ordering step 1; Phase 2 Analyze Rulings). Frozen PR #264 / `b565e932` is planning evidence only until landing. At recon baseline HEAD there is no SignalR client/server code and no `@microsoft/signalr` in the lockfile (`recon-DEV-422:54-66`). Existing upstream DEV-330 constitution-departure owner checkbox and Proposed ADR-0029 remain unresolved; DEV-422 does not invent a second constitution checkbox.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-422/
├── spec.md
├── plan.md
├── tasks.md
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
└── DRAFTING_RECEIPT.md
```

`research.md`, `data-model.md`, `quickstart.md`, `contracts/`, and `design.md` are not produced. `AGENTS.md` is not updated: outside the feature directory and outside the named file set. No ui tag — Palette design.md is not required.

### Source Code (repository root)

```text
web/package.json                                     # pin exact @microsoft/signalr 10.0.x
web/package-lock.json                                # lockfile for that pin

web/src/library/useEnrichmentStatus.ts               # new — instance-owned connection/timer/registry; injected factory (default private); narrow subscribe return
web/src/library/useEnrichmentStatus.test.tsx         # new — hand-written connection-fake lifecycle/event/freshness proofs (no new package)

web/src/main.tsx                                     # root QueryClientProvider host; pass subscription seam via existing props
web/src/App.tsx                                      # shell wiring; pass subscription seam to details; import placeholder preservation
web/src/App.test.tsx                                 # focused App proofs (additive)

web/src/library/libraryQueries.ts                    # remove browse polling; mutation invalidation
web/src/library/libraryQueries.polling.test.tsx      # repurpose — no-polling / event reconciliation; concrete aria-live=polite assert
web/src/library/LibraryView.retry.test.tsx           # update — drop interval assertions

web/src/features/movie-details/movieDetailsQueries.ts  # subscribe/unsubscribe via passed seam; one-way invalidate
web/src/features/movie-details/MovieDetailsView.tsx  # receive subscription seam via existing props; one-way details bridge
web/src/features/movie-details/MovieDetailsView.test.tsx  # focused details bridge / freshness / no-event start+reconnect proofs

web/src/api/openapi.json / web/src/api/types.ts      # consume landed DEV-330 generated schema only via chain
web/e2e/import.e2e.ts                                # only if required + landed hub available
```

**Structure Decision**: Match brief Q6 placement under existing `web/src/library/` with bounded shell and movie-details edits. Instance-owned connection/timer/registry inside `useEnrichmentStatus.ts`; narrow subscription seam passed through existing `main.tsx` → `App.tsx` → movie-details props — no new context/provider/store/event-bus module, module-global mutable registry, second connection, or reverse invalidation cycle. Nested details QueryClient retained (brief Q4; Phase 2 plan-challenge item 2).

## Design (from settled brief + Phase 2 plan-challenge)

1. **Ownership and dependencies (Q1)**: Frontend-only SignalR client; hub is DEV-330. ADR-0022 accepts transport. Constitution polling-text cleanup out of scope.
2. **Contract and types (Q2)**: Route/event/payload frozen as above. Owner checkbox for generated vs hand-typed. Runtime validation with existing Zod: exactly one arg, non-null object, finite integer id, four exact status strings; malformed matrix as in brief item 3; no positivity rule or duplicate DTO. Hand-written connection fake, not MSW SignalR emulation or a new package. Lost events accepted; recovery is HTTP.
3. **Placement and lifecycle (Q3 + challenge item 2)**: `useEnrichmentStatus.ts` keeps connection, timers, and callback-subscription registry per mounted root hook instance. Inject factory via hook parameter/default; default helper private. Return narrow subscribe seam; pass through existing main/App/details props; subscribe/unsubscribe in existing movie-details files with cleanup. Publish valid events and start/reconnect recovery independently of browse-cache presence/status. URL/credentials; `withAutomaticReconnect` + outer retry from ASSUMPTIONS.md; single start/retry owner; StrictMode-safe cleanup. No global mutable registry, new context/store/module, second connection, or reverse cycle. T003 teardown asserts zero fake live connections/pending timers before restoring fake timers.
4. **Cache and recovery (Q4 + challenge items 1, 5, 6)**: Validate → patch existing browse rows → invalidate browse; coalesce refreshes without swallowing a valid event that arrives during an older refetch (R1-start → E2 → stale R1 complete → fresh convergence for browse and mounted details); one-way details bridge; start/reconnect invalidate including Pending and reconcile mounted details with no event (including absent-browse ids); failed refetch retains event patch until subsequent event/recovery — no rollback; no recommendation caches.
5. **Polling removal (Q5 + challenge item 5)**: Remove interval machinery; keep optimistic Pending helpers; mutation success invalidates browse; repurpose polling/retry tests; preserve visuals; T010 concretely asserts `aria-live="polite"`.
6. **Files, pin, loop (Q6 + challenge item 4)**: Exact file set; `--save-exact` 10.0.x pin at pickup with no ranges at package/lock root; post-pin `npm audit` introduced-finding disposition (no blanket-fail unrelated historical advisories); build size delta without new tooling/budget; browser imports + `import type`; no Node-only runtime transport; High/Medium block; two review rounds; frozen scope sentence. Lows deferred per brief stay deferred.

### Plan and task ordering (brief)

1. Carry Q2 owner checkbox into Phase A artifacts and PR; before Phase B verify resolution, landed approved upstream constitution disposition/amendment, upstream DEV-330 hub/generated-chain landing, and contract drift.
2. Verify and pin the compatible published exact npm patch (`--save-exact`) and lockfile; record audit disposition and later build size delta.
3. Implement instance-owned lifecycle hook (injected factory / private default), narrow subscription return, root mounting, and hand-written connection-fake tests (including teardown assertions).
4. Implement Zod-validated event cache updates, non-swallowing coalesced invalidation, details bridge via passed subscription seam, start/reconnect reconciliation (including no-event details for absent-browse ids), and R1/E2 freshness proofs.
5. Remove enrichment polling; migrate optimistic import and retry/request/match reconciliation and repurpose tests with concrete `aria-live="polite"` assertion.
6. Run web verification (lint/Vitest/build, audit disposition, size delta, browser-import check) and report live-hub integration limitations explicitly.

## Complexity Tracking

> Pending upstream constitution disposition is carried honestly, not claimed clear. Accepted ADR-0022 notes a pending constitution amendment; constitution polling text remains authoritative until that amendment lands; upstream DEV-330 Gate 1 constitution-departure owner checkbox and Proposed ADR-0029 are unresolved (`recon-DEV-422:130-132`; CONCLUSIONS Phase 2 Analyze Rulings). This plan does not claim present constitutional compliance over `.specify/memory/constitution.md:507,534,545-565`, does not edit the constitution, and does not add a duplicate constitution owner checkbox — only the existing DEV-422 payload-type checkbox. Hook placement, retained nested details client, and the ticket-approved npm pin introduce no additional numbered-principle violations requiring Complexity Tracking rows.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Upstream constitution polling text vs Accepted ADR-0022 SignalR transport (pending amendment) | Transport accepted by ADR-0022; constitution amendment and DEV-330 Gate 1 disposition remain upstream ownership | Editing constitution or inventing a second DEV-422 constitution checkbox would expand scope and duplicate the existing upstream gate |

## Phase boundaries

- **Phase A (this ask)**: Spec Kit docs under `specs/DEV-422` only (`spec.md`, `plan.md`, `tasks.md`, `DRAFTING_RECEIPT.md`). No freeze, publish, gates, or implementation. Keel owns read-only speckit-analyze and brief alignment next. Gate 1 remains closed pending owner checkbox.
- **Phase B**: After Gate 1 (owner checkbox answered); landed approved upstream DEV-330 constitution disposition/amendment; and verified landed DEV-330 hub implementation + generated OpenAPI/types with stop-on-divergence. Then Build implements per `tasks.md`; Cog refactor; Gauge gates (including recorded propertyTests opt-out — never PASS).

## Keel plan-challenge closure (2026-10-26)

Per `brief.md`, Phase 2 closure and conditional plan freeze: all three axes adjudicated, Quill correction round 1 verified, no unresolved High/Medium plan findings. This plan is frozen for owner Gate 1 review; this final status supersedes the earlier drafting-stage no-freeze status. See `PLAN_CHALLENGE_ADJUDICATION.md` for all 23 dispositions. No second correction round, production edits, or gate runs. Gate 1 remains closed pending the existing payload-type owner checkbox; all Phase B upstream landing/constitution/drift prerequisites remain in force. This is readiness for spec-PR preparation and owner review, not Gate 1 approval or implementation authorization.
