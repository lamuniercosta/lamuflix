# Implementation Plan: API failure toasts

**Branch**: `feature/567-spec` | **Date**: 2026-11-23 | **Spec**: `specs/DEV-567/spec.md`

**Worktree**: `F:/Dev/LamuFlix.worktrees/feature-567-spec`

**Input**: Grill-closed `brief.md` (Q1–Q8); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-567`, `recon-DEV-567` (intake HEAD `c7d5fb025b3793680a02fdf5f1785553becf9343`, addendum lines 4–30); `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: **PLAN FROZEN** (2026-11-23) after three-axis disposition in [PLAN_ADJUDICATION.md](PLAN_ADJUDICATION.md). Ready for ordinary Gate 1 ruling. T000 requires a verified user-merged Spec PR before Phase B.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md`, or `contracts/` — decisions frozen in the brief; public HTTP behaviour unchanged (schema documentation only).

## Summary

Deliver a dependency-free app-wide API failure toast host, wire reporting at React Query global cache callbacks and the three raw subtitle acquisition calls, share one normalization helper (no global fetch interceptor), and document already-emitted optional `reason` / `traceId` on the C# OpenAPI `ProblemDetails` projection then regenerate `web/src/api/openapi.json` and `web/src/api/types.ts`. Preserve runtime HTTP responses, routes, query retry policy, and inline subtitle behaviour. Prove the Q7 evidence matrix.

## Technical Context

**Language/Version**: TypeScript / React 19 SPA under `web/`; C# / .NET 10 `LamuFlix.Api` for the OpenAPI schema-only transformer (`specs/PRODUCT.md` §1; brief approach steps 1–5).

**Primary Dependencies**: Existing stack only — React Query already in `web/src/main.tsx`, openapi-fetch client in `web/src/api/client.ts`, Vitest/RTL/MSW, Playwright, ASP.NET Core OpenAPI transformers. **No new npm or NuGet dependency** (ticket; `PRODUCT.md` §5 item 1).

**Storage**: N/A — ephemeral client toasts; no database schema change.

**Testing**: Vitest/RTL + existing MSW server `web/src/test/server.ts` and nearby subtitle fixtures; named QueryClient suite `web/src/api/queryClient.test.tsx` exercising both configured `QueryCache` and `MutationCache` with representative forced HTTP failures, rejected fetch, one toast with diagnostics, and exclusion of unrelated programming exceptions (spec US1/US2; Q5/Q7); host Vitest suite for lifecycle/a11y/reduced-motion (styling intent only — not browser layout); one Playwright scenario following `web/e2e/dev525-subtitle-search.e2e.ts` covering SMOKE-001 (toast + inline), viewport-fit at a narrow viewport, and SMOKE-002 (dismiss + focus) in that single scenario; web lint/typecheck/test gates; mandatory scoped .NET static gates, `dotnet format --verify-no-changes`, and `LamuFlix.ArchitectureTests` inside `dotnet test` for the C# schema transformer diff, with other harness gates by applicability (`brief.md` ruling 7).

**Target Platform**: Home web via nginx `/api` proxy (port 3100) and Vite `http://localhost:5173` CORS path noted in ticket Context; toast must cover both HTTP problems and transport failures with no problem body.

**Project Type**: Existing .NET API + Vite/React web app. No new project or top-level folder.

**Performance Goals**: Toast cap of three; five-second unpaused auto-dismiss. No invented latency SLA beyond Patron Q6 lifecycle.

**Constraints**: Charter §2.3 items 1–6 deliberately ruled (Q1–Q8). Zero runtime API payload/route/status change. Keep `queries: { retry: false }`. No global fetch interceptor. Closing bar: Critical/High/Medium and failed mandatory ACs block; every Low explicit disposition; accepted in-scope defects close; Size M Risk/Standards/Spec; two-round review cap; remaining blockers to Bernstein (`brief.md` Frozen scope / Closure).

**Scale/Scope**: Size M; one host + two reporting boundaries + schema documentation + Q7 evidence. Parent DEV-490.

## Constitution Check

*GATE: Planning alignment checked before Phase 0 research. Re-check after Phase 1 design. Executed gate evidence is Phase B only — not claimed by this draft.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No owner structural checkbox from this draft.

| Principle / rule | Gate | Plan disposition (planning alignment only) |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation | **PASS** — no new Core ports; web-only toast + API OpenAPI schema transformer only. |
| II (Explicit Handlers) | Handlers + AddHandler | **N/A** — no new API handlers. |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — no Movie mutation. |
| V (Validated inputs / errors) | ProblemDetails + traceId | **PASS** — runtime already emits `reason` / `traceId`; this ticket documents them in OpenAPI only; toast consumes diagnostics without leaking `detail` or exception text (Q1/Q5; Principle V error-shape intent). |
| VI (Observability) | Safe diagnostics | **PASS** — surface existing `traceId` for log correlation; never fabricate trace IDs (Q1). |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **PASS** — no LocalPlay, secrets, or Process.Start. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — no new domain glossary term; spec-local vocabulary only; no Quill ADR. |
| IX (Test Pyramid) | Meaningful layers | **PASS** — Vitest/RTL+MSW for host/helper/QueryClient caches/raw paths; one Playwright e2e; .NET gates for schema transformer. |
| Tech stack / API docs | OpenAPI → openapi.json → types.ts | **PASS** — forced contract chain repair documents optional fields; never hand-edit `types.ts` (Q2/Q3; `PRODUCT.md` §1). |
| Governance / Static-Analysis Gates | Honest exits | **PASS (planning alignment)** — Phase B must name and run mandatory scoped static gates (`./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, `./scripts/run-jetbrains-inspectcode.ps1`), `dotnet format --verify-no-changes`, and `LamuFlix.ArchitectureTests` inside `dotnet test`; mutation and other gates by `harness.yml` applicability; never fold non-pass into PASS; never lower thresholds. **Executed gate evidence is not claimed here.** |

### Complexity Tracking

No constitution violations requiring justification. No new dependency. Schema transformer follows existing `Program.cs` v1 transformer registration pattern (recon addendum lines 26–29).

## Approach

- **Phase A (complete)**: Quill drafted the Spec Kit artifacts. Keel completed read-only analysis and the three-axis challenge. Bernstein coordinates the Spec PR, Rigger prepares it, the user merges it, and Keel records the Gate 1 receipt in T000.
- **Phase B pickup**: Fresh drift recon if needed (read-only T001); any authorized drift amendment is a separate subsequent step; exact path confirmation for Quill-named helpers; Gauge baseline. Pickup **records** any known discarding-adapter boundary (T001 is read-only and does not edit). **T010** owns any authorized minimum adapter edit when that boundary is already identified; STOP for targeted recon only when a required boundary remains unidentified.
- **Task order (`brief.md` Approach steps 1–5)**:
  1. T001 records the app-generated v1 export candidate from recon. T002 verifies the full invocation and configuration or stops for Wisp recon. Register `ProblemDetailsSchemaTransformer` alongside the existing v1 transformers. Target exact `ProblemDetails`, leaving `HttpValidationProblemDetails` outside this change. Check the exported v1 schema for both optional strings before generating TypeScript. Verify the narrow schema diff, runtime payload invariance and repeatable JSON/TypeScript regeneration.
  2. Add app-wide toast host + focused host Vitest/RTL lifecycle tests under `web/src/ui/` (Quill-named); house tokens/motion rules in `web/src/index.css`; mount in `web/src/main.tsx` above routes. Vitest may check styling intent; browser viewport-fit is not measured there.
  3. Use `web/src/api/queryClient.ts` to create the real QueryClient used by `main.tsx` and `web/src/api/queryClient.test.tsx`. Call the shared reporter once per terminal, classified API failure from each configured cache. Only known HTTP failures or scoped, explicitly identified fetch rejections qualify. Ignore unknown throws and intentional cancellation. T001 inventories error subscribers and any discarding boundary. T010 owns the bounded metadata-preserving edit and stops for targeted recon only if a needed boundary is unidentified.
  4. Report raw catalog, search and download failures after the existing currency checks and applicable cancellation checks. Keep `readSubtitleProblem` for the inline UI, including its existing status/reason/quota interpretation. Use actual response status for the toast when available, including HTTP errors whose body cannot be parsed. T011/T012 cover both surfaces and retain one toast per execution, including each 429.
  5. Add one Playwright failure scenario under `web/e2e/` (Quill-named; follow DEV-525 pattern) covering SMOKE-001 toast+inline, viewport-fit at a narrow viewport, and SMOKE-002 dismiss+focus in that single scenario (no second scenario; UI contract unchanged). Run the complete evidence matrix and applicable gates (mandatory scoped static gates, format, ArchitectureTests; other gates by harness applicability).
- **Reporting and host**: Keep pure diagnostic normalization and queue publication in the shared helper. Only render title, status, reason and trace ID from classified failures. Use text rendering and wrap long diagnostic values. Feed an app-scoped in-memory queue to a subscriber host and create fresh queues in tests. Publish from the reporting boundary rather than a React effect. Concurrent failures remain distinct and evict oldest first with timer cleanup.

- **Constraints**: Existing dependencies and routes, current HTTP payloads and retry policy, and existing inline subtitle behavior govern the implementation. The generated v1 document is the source for TypeScript. Gate 1 precedes Build and runtime verification.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-567/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── ticket-text.txt
├── spec.md
├── plan.md              # This file (worktree / Spec PR copy)
├── tasks.md
├── PLAN_ADJUDICATION.md
├── DRAFTING_RECEIPT.md
└── checklists/
    └── requirements.md
```

Private Spec Kit mirror: `C:\Users\lamun\AppData\Local\LamuFlix\artifacts\LamuFlix\DEV-567\plan.md` (same content). No `research.md` / `data-model.md` / `contracts/` — decisions frozen; no public API behaviour delta.

### Source Code (authorized boundary — brief approach; Quill names within listed areas)

```text
src/LamuFlix.Api/
├── Program.cs                              # EXTEND — register ProblemDetails schema transformer in AddOpenApi("v1", ...)
├── ProblemDetailsSchemaTransformer.cs      # NEW — optional reason/traceId on ProblemDetails schema (Quill-named)
├── EnrichmentStatusSchemaTransformer.cs    # PATTERN ONLY
├── EnrichmentHubSchemaTransformer.cs       # PATTERN ONLY
├── OpenApiDocumentNormalizer.cs            # PATTERN ONLY
└── ExceptionHandling/
    ├── GlobalExceptionHandler.cs           # FACT — runtime reason emission (do not change behaviour for this ticket)
    └── ProblemDetailsTraceId.cs            # FACT — runtime traceId emission (do not change behaviour for this ticket)

web/src/
├── main.tsx                                # EXTEND — mount toast host; use configured QueryClient factory
├── index.css                               # EXTEND — toast host tokens / reduced-motion rules
├── api/
│   ├── openapi.json                        # REGENERATE from app-generated v1 export
│   ├── types.ts                            # REGENERATE via npm run generate:api (never hand-edit)
│   ├── client.ts                           # existing openapi-fetch client (no global interceptor)
│   ├── queryClient.ts                      # NEW — configured cache callbacks and retry policy
│   ├── queryClient.test.tsx                # NEW — Vitest/RTL+MSW through the configured caches
│   ├── reportApiFailure.ts                 # NEW — shared normalize + report helper (Quill-named)
│   └── reportApiFailure.test.ts            # NEW — normalization/reporting unit coverage (Quill-named)
├── ui/
│   ├── ApiFailureToastHost.tsx             # NEW — host + stack/dismiss/live region (Quill-named)
│   └── ApiFailureToastHost.test.tsx        # NEW — lifecycle/a11y/reduced-motion host tests (Quill-named; not browser layout)
├── features/playback/
│   ├── useSubtitleAcquisition.ts           # EXTEND — explicit helper at catalog/search/download boundaries
│   ├── subtitleAcquisition.ts              # existing problem reader for inline status/reason/quota
│   └── subtitlesTab.test.tsx               # EXTEND — MSW failure/toast coverage for three raw calls
└── test/
    └── server.ts                           # existing shared MSW server

web/e2e/
└── dev567-api-failure-toast.e2e.ts         # NEW — single scenario: SMOKE-001 toast+inline + narrow viewport-fit + SMOKE-002 dismiss+focus (Quill-named)
```

**Structure Decision**: Stay inside existing `LamuFlix.Api` OpenAPI area, `web/src/ui/`, `web/src/api/`, playback feature, and `web/e2e/`. No new top-level folder or layer. File names and the authorized boundary follow the brief, including its Phase 2 step 6 amendments. Use the existing .NET test project identified at T001 if mutation applicability calls for a focused transformer regression (T003/T014).

## Phase 0 / Phase 1 note

Decisions are frozen in `brief.md` / `CONCLUSIONS.md` / `ASSUMPTIONS.md`. No separate `research.md`. No new public HTTP contract beyond documenting existing optional extensions — no `contracts/` folder. Agent context plan pointer update is deferred to Gate 1 / implement pickup if required by harness scripts; Quill does not edit standing `AGENTS.md` / `CLAUDE.md` SPECKIT markers unless an ask names them (role: no README; mirror DEV-545 practice of documenting plan path here only). The plan challenge is closed. T000 owns the Gate 1 receipt.
