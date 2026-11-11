# Implementation Plan: Subtitle provider and BS Player-compatible sidecar

## Current Phase B pickup authority (2026-11-11)

Route authority: CONCLUSIONS.md Phase B T011 / T012 Scope & API Shape Ruling (2026-11-11), recorded first in brief.md Phase B sidecar route drift alignment. DEV-453 uses `POST /movies/{id}/subtitles/prepare` and `GET /movies/{id}/subtitles/sidecar` with raw `{id}` parsing. Preserve delivered DEV-454 `GET /movies/{id:int}/subtitles` (`SubtitleTrackDto[]`) and `GET /movies/{id:int}/subtitles/{trackId}/content` (`text/vtt; charset=utf-8`). This bounded route alignment changes no other settled behavior and requires no ticket change or owner checkbox.

This section supersedes earlier Phase A pending-owner wording and external host/protocol/vector pickup STOPs below. Basis: CONCLUSIONS.md T000 receipt and Patron pickup ruling committed at `261290ff`, applied first in brief.md Authoritative Phase B pickup adjudication addendum. T001/T001b are complete with external STOPs disposed by that ruling, not by claims of external verification. T001c local Principle I document work is complete in constitution v1.14.0; normal delivery review/approval remains required. ADR 0038 stays Proposed; T002 atomic whitelist/port implementation is still future work. Principle VII owner approval is recorded and its scoped amendment is now applied locally in constitution v1.15.0 (see brief.md Principle VII amendment execution). T003/T004 retain their implementation dependencies and normal delivery review/approval; the former missing-amendment prerequisite is resolved locally. No runtime or gate PASS is asserted; speckit prerequisite Could not run due its branch-name guard, with bounded manual pickup reanalysis performed instead.

- Provider options are explicit: Subtitles:OpenSubtitles:BaseUrl, DownloadAllowedHosts, ApiKey, UserAgent, MaxDownloadBytes and RequestTimeout; no hardcoded runtime fallback defaults. Missing/invalid acquisition settings return prepare-time 409 under the approved Principle VII path under the scoped constitution v1.15.0 exception. https://api.opensubtitles.com is sample/operator configuration only. T007 verifies the adopted JSON /api/v1/subtitles search and /api/v1/download temporary link contract and raw UTF-8 SRT link response with WireMock.NET, including HTTPS/host checks, disabled redirects, all 3xx rejected as 502, no retry and all existing content/timeout/error bounds. A later observed live protocol mismatch remains an unresolved decision; do not silently add redirects or decompression.
- T005 uses deterministic synthetic reference vectors and properties for unsigned 64-bit file length plus little-endian 64-bit word sums of first and last 65,536 bytes, modulo 2^64, formatted as 16 lowercase hex digits; eligible length is at least 131,072 bytes. An all-zero 131,072-byte input yields 0000000000020000. External vectors are no longer a pickup prerequisite under Patron ruling. Movie.Title and the delivered authorized seekable reader implementation are confirmed; retain Stream abstraction without widening the port.
- Extend the delivered DEV-454 subtitle surfaces and generated contract chain, preserving existing listing/content/browser contracts. Handler registration is Api/HandlerRegistration.cs; existing SubtitleEndpoints and ApiEndpoints /api composition are pickup facts. T002 appends only |ISubtitleProvider|ISubtitleSidecarStore before )$ after the existing 17 whitelist alternatives (19 total), with port types, preserving all existing test rules. Existing sibling-feature namespace isolation applies; no cross-feature references.
- T016/T017 document demo read-only library bind mount and daily-driver host Library:RootPath with process write permissions; daily-driver Compose contains infrastructure services and no library mount. T022 separately requires live provider, writable-save, player-autodetection and read-only-serving receipts; absent prerequisites remain Could not run and acceptance stays open.

**Branch**: `feature/453-spec` | **Date**: 2026-11-09 | **Spec**: `specs/DEV-453/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q12) + Authoritative analyze adjudication addendum + Authoritative Plan Challenge adjudication addendum (2026-11-10); `CONCLUSIONS.md` corrected Patron analyze rulings; `ASSUMPTIONS.md`; `spec.md`; Keel-authored `docs/adr/0038-subtitle-provider-and-sidecar-boundary.md` (Proposed); Maestri notes `DEV-453`, `recon-DEV-453`, `adjudication-DEV-453-Keel`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (gates cite only; Quill does not edit constitution or ADRs). Grill commit: `cd575e55`.

**Status**: Draft — Plan Challenge round 1 planning repairs against brief Authoritative Plan Challenge adjudication addendum (2026-11-10). **blocked: structural (C4/VII)**. Gate 1 stays closed until the owner structural checkbox in `spec.md` is answered. No PLAN FROZEN / analyze-clean / Gate 1 clearance / runtime PASS claim. Affected ValidateOnStart acquisition-options implementation remains blocked until owner approval and scoped constitution/governance amendment; Principle I remains independent.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md` (Quill), plus authorized append-only root `CONTEXT.md` five-term compliance edit (C4/VIII). No `research.md`, `data-model.md`, `quickstart.md`, or `contracts/` — every technical choice is ruled in the brief; public routes/DTO/statuses are encoded in `spec.md` and OpenAPI generation tasks (precedent DEV-456 / DEV-438). `ASSUMPTIONS.md` already exists from grill; Quill does not rewrite it. ADR 0038 drafted by Keel — Quill does not edit ADR bodies; status remains **Proposed** until Gate 1 / governance acceptance lifecycle.

## Summary

Ship one OpenSubtitles.com REST subtitle provider for personal non-commercial use, with server-ordered language preferences, exact-basename BS Player-compatible SRT sidecars, explicit prepare + existing-track serve HTTP operations, writable daily-driver acquisition (opt-in) and read-only demo serving, regenerated OpenAPI/TS contracts, focused tests, and separate live runtime receipts — without new packages/schema/layers, without widening `ILibraryFileReader`, and without LocalPlay/launch changes.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`TreatWarningsAsErrors=true`, nullable enabled, CPM).

**Primary Dependencies**: Existing pins only (ASP.NET Core Minimal APIs, existing HTTP client factory patterns, WireMock.NET in tests). **No new NuGet/npm packages** (Q7; PRODUCT §5 item 1). OpenSubtitles.com is an external personal-use API, not a repo package.

**Storage**: Library filesystem sidecars only (exact-basename `.srt`). **No database schema or migration** (Q7; PRODUCT §5 item 3).

**Testing**: Existing xUnit v3, Shouldly, NSubstitute, FsCheck, WireMock.NET, real temporary-filesystem fixtures for containment/atomic writes. No Gherkin. No real credentials in fixtures. No driver mocks for translation/serialization-dependent behaviour.

**Target Platform**: .NET server (ASP.NET Core Minimal API); Compose read-only demo library mount; writable daily-driver library mount for acquisition.

**Project Type**: Ports and adapters in existing Core / Infrastructure / Api projects + generated web contract artifacts (no SPA workflow).

**Performance Goals**: Personal-library scale; bounded download size/time via explicit configuration; no whole-file subtitle buffering beyond configured caps.

**Constraints**: Two named narrow ports only; `ILibraryFileReader` stays read-only; no new packages/projects/layers/schema; no LocalPlay/`Process.Start`/secrets-in-repo; never hand-edit generated OpenAPI/TS; never lower harness thresholds; no guessed numeric safety defaults; TimeProvider / seeded Random / `const` static-init hygiene where applicable; no explanatory comments outside allowed exemptions.

**Scale/Scope**: Size L; backend subtitle prepare/serve; three-axis review required (Q11).

## Constitution Check

Gates checked against `.specify/memory/constitution.md` (worktree current). Brief Authoritative analyze adjudication addendum supersedes the original Q12 no-structural-blocker / no-constitution-departure / feature-local-only glossary claims for analyze consistency. C1/V, C2/VI, and C4/VIII are resolved with zero departure; **C4/VII remains `blocked: structural`** with the unchecked owner checkbox in `spec.md` (Gate 1 closed until user approval). Quill does not check that box or amend the constitution. Adding two Core ports requires ADR (0038 already Proposed) plus Principle I named-port enumeration/governance and ArchitectureTests whitelist append — residual compliance execution, independent of C4/VII.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; adding a port requires an ADR; closed enumeration; architecture enforcement | **OPEN — residual compliance required (not implementation PASS)**: brief Q7 / ADR 0038 authorize `ISubtitleProvider` and `ISubtitleSidecarStore` only. ADR 0038 already authored (Proposed). Before/with port implementation: Phase B T001/T001c must verify Principle I feature-folder enumeration and ArchitectureTests namespace-isolation rules; Keel-owned constitution named-port enumeration extension adding only those two ports with required governance/feature-folder compliance, plus ArchitectureTests `PortWhitelistPattern` append-only `\|ISubtitleProvider\|ISubtitleSidecarStore` (exact insertion confirmed at pickup) landed atomically with the port types. Handlers resolve movie/video/library only via shared `Core/Ports` and `Core/Domain` — no sibling feature-folder cross-references. Do **not** backfill historically omitted ports. Keep `ILibraryFileReader` read-only (ADR 0037) and use it for authorized moviehash head/tail reads; `ISubtitleProvider` is data-only (hash/size/title/preferences — never path/stream). **STOP** before new Core feature folder/ports if enumeration/isolation unconfirmed. Clear STOP prerequisites; ADR existence alone is not Principle I PASS. |
| II (Explicit Handlers / Decorators) | sealed commands/queries; sealed handlers; AddHandler + decorators | **SCHEDULED (not PASS)** — prepare/serve are Core use cases: `sealed record` commands/queries, `sealed` Core handlers, `services.AddHandler<…>` with Tracing → Logging → Validation → handler decorators; thin TypedResults endpoints only (no MediatR/repository/"services" layer; Q7). Endpoint-local orchestration is not a substitute (constitution Principle II:373–381). |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter/sort surface. |
| IV (Domain transitions) | Movie mutators | **PASS** — sidecar files are not Movie aggregate mutations; no enrichment state machine change (Q7). |
| V (Validated inputs / errors) | FluentValidation + single ProblemDetails mapper | **SCHEDULED (resolved C1/V + Plan Challenge)** — raw `{id}` capture/parse (or equivalent reachable binding) so transport malformed/unparseable route ID → **400** ProblemDetails + `traceId`; well-formed absent → **404**; sealed handlers + FluentValidation decorator for validly bound commands/queries; `ValidationException` → **422** via the single ProblemDetails + `traceId` mapper (never remapped to 400). Do not claim all validation is 400; do not invent unreachable GET validation scenarios. All declared prepare/serve failures (`403`/`404`/`409`/`429`+optional Retry-After/`502`/`504`) are typed Core exceptions/failure categories through that same mapper — no new result layer, no endpoint-local status mapping. Sanitized errors; no path/secret leakage (brief addendum C1/V; Plan Challenge items 2.4, 5.3, O1; Principle V:451–466; Q6/Q10). |
| VI (Observability) | Telemetry / health | **SCHEDULED (resolved C2/VI + Plan Challenge L1)** — functionality-named TelemetryConstants; structured events carrying movie id; ServiceDefaults/Infrastructure-owned wiring (hosts MUST NOT duplicate). ONE always-Healthy optional-provider local health policy with sanitized metadata `ready` or `disabled_or_unconfigured` (including enabled-but-invalid/missing acquisition config), zero network/quota calls, full enabled/disabled/configured/invalid test matrix, and **explicit exclusion from `/health/live`**. Does not assert upstream reachability or change other dependency health results. Hygiene: never log Api-Key, paths, or subtitle content (brief addendum C2/VI; Plan Challenge item 7; Principle VI:478–490/623–624; Q5/Q8/Q10). |
| VII (Configuration / LocalPlay / Process.Start) | LocalPlay gated; secrets local; ValidateOnStart | **BLOCKED structural / UNRESOLVED owner checkbox (not PASS)** — LocalPlay independence, secret-injected ApiKey, and no Process.Start/launch changes remain brief-aligned (Q7, Q10). C4/VII is `blocked: structural` per brief addendum item 3 / Plan Challenge item 3 / corrected Patron CONCLUSIONS item 3: Principle VII ValidateOnStart vs Q10 prepare-time 409 + no guessed numeric defaults. Unchecked owner checkbox lives in `spec.md` **verbatim** (`SubtitlesOptions / OpenSubtitlesOptions`) and must appear on the spec-PR body; Gate 1 closed until user approval. Do not implement or amend constitution on assumed approval; reject conditional-startup Option B. If/when approved, the scoped Principle VII constitution/governance amendment MUST include both the narrowly scoped ValidateOnStart departure **and** append the new subtitle options record(s) to Principle VII's closed options-record list. Implementation types remain functionality-named (`SubtitlesOptions` / `SubtitleProviderOptions`) while vendor config nesting `Subtitles:OpenSubtitles:*` is retained. Preserve Q10 / no numeric defaults. T001c covers Principle I only and cannot close this. See DRAFTING_RECEIPT unresolved C4/VII. |
| VIII (Ubiquitous Language) | CONTEXT.md | **SCHEDULED compliance edit (resolved C4/VIII)** — append-only repository-root `CONTEXT.md` entries for Ordered language preferences, Selected subtitle, Sidecar, Preparation, Existing track (term/definition/_Avoid_:), preserving all prior content, in the same spec PR introducing the terms. Authorized by brief addendum item 4 / corrected Patron item 4; supersedes only the original Q12 feature-local-only file restriction. Not deferred to delivery. Functionality-named C# options/telemetry identifiers satisfy Principle VIII with zero vendor-allowlist departure (Plan Challenge item 4). |
| IX (Test Pyramid) | Real I/O where behaviour depends on it | **PASS** — real temp-filesystem sidecar tests; WireMock.NET for HTTP provider; no FS-auth mocks for containment proofs (Q9). |
| API / contract rules | MapXxxEndpoints; generated OpenAPI/TS | **SCHEDULED** — proposed `SubtitleEndpoints` thin TypedResults surface for brief-relative routes; exact existing DI/endpoint-mapping entrypoint filename deferred to Phase B Wisp T001 (recon does not retain `ApiEndpoints.cs` / `/api` grouping as facts). C# → `openapi.json` → `types.ts`; never hand-edit (Q6). |
| Governance / Static-Analysis Gates | Honest exits; never lower thresholds | **OPEN for Principle I governance/whitelist and blocked C4/VII owner checkbox; otherwise Phase B / Build honesty**: report actual exits; SKIPPED/SKIP/N/A/Could not run never PASS; live receipts separately mandatory (Q9, Q11). Gate 1 remains closed while C4/VII is unchecked.

### Complexity Tracking

C4/VII is an explicit unresolved owner structural departure checkbox, not a Quill invention or silent PASS. Principle I residual execution (constitution/governance + atomic port/whitelist for the two brief-approved ports) remains independent and **not** marked PASS. ADR **0038** already authored by Keel; Quill does not author, reauthor, or edit ADR/constitution on assumed C4/VII approval.

## Approach

- **Phase A (this draft)**: Quill writes spec/plan/tasks/checklist/receipt from brief/CONCLUSIONS/ASSUMPTIONS/ADR 0038/recon facts only. Keel owns read-only `/speckit-analyze` and plan challenge/freeze. No production edits, tests, gates, commits, or PR here (Q9, Q12).
- **Phase B pickup (after Gate 1)**:
  - **Wisp**: fresh recon of delivered DEV-457 / DEV-454 contracts; actual DI/endpoint mapping entrypoint filenames; current OpenSubtitles headers/error schema; exact API/download hosts + protocol compatibility (STOP if redirects required); hash reference vectors; `ILibraryFileReader` head/tail read/seek contract; Movie primary-title property; download raw-vs-wrapper payload shape; Principle I feature-folder enumeration + ArchitectureTests namespace-isolation rules; ArchitectureTests whitelist current pattern/count for append plan. Facts only — no task invention.
  - **Keel**: pickup cross-artifact reanalysis / authorized task-drift adjudication; Principle I constitution enumeration + governance/feature-folder compliance for `ISubtitleProvider` and `ISubtitleSidecarStore` only (or exact-equivalent verified evidence); verify ADR 0038 present (remains Proposed until acceptance lifecycle; accepted historical grill-closure context — no Quill ADR rewrite). Feature implementation MUST NOT start until pickup STOP prerequisites clear.
- **Dependency-ordered implementation (brief Q9)**:
  1. Pickup recon + Principle I compliance prerequisites (including feature-folder/isolation confirmation before new folder/ports).
  2. Approved functionality-named DTO/options/models (`SubtitlesOptions` / `SubtitleProviderOptions`) + two Core ports (atomic with whitelist code); data-only provider seam.
  3. Hash via authorized `ILibraryFileReader` + deterministic language selection tests, then HTTP provider adapter (WireMock.NET) with pre-request HTTPS/host checks, redirects disabled, all-`3xx`→502, and minimal downloaded-SRT criteria.
  4. Contained sidecar reads/atomic writes (real temp FS), then orchestration + API integration (raw `{id}` capture/parse; typed Core failures through single mapper).
  5. Regenerate OpenAPI/TS; document demo/writable setup; run applicable gates.
  6. Collect live configured-key / writable-save / BS Player autodetection / read-only-serving receipts (or `Could not run`).
- **API / handlers**: thin TypedResults endpoints dispatch sealed prepare command / serve query to sealed Core handlers registered via `AddHandler` (Tracing → Logging → Validation → handler). Handler flow: authorize movie/video/library → existing-sidecar check → (if absent) acquisition config validation → moviehash via `ILibraryFileReader` → data-only provider lookup → deterministic selection → pre-validated allowlisted download (no auto-redirect) → atomic sidecar create → outcome DTO with `selectedLanguage` always set on `saved`. GET path: authorize → open exact-basename sidecar → bounded `application/x-subrip` bytes + `nosniff` + safe-basename `Content-Disposition: attachment`. Single ProblemDetails exception mapper with `traceId`; typed Core failure categories only. Explicit transport malformed-id **400** via raw capture/parse; FluentValidation `ValidationException` stays **422** (never remapped to 400); provider timeout→504 vs caller abort as cancellation; OpenAPI/tests include reachable application-validation 422 metadata only (resolved C1/V + Plan Challenge).
- **Observability (Principle VI)**: Functionality-named TelemetryConstants and structured movie-id events; ServiceDefaults/Infrastructure-owned wiring. ONE always-Healthy optional-provider local health policy with sanitized `ready` / `disabled_or_unconfigured` metadata, zero network/quota calls, full enabled/disabled/configured/invalid matrix, excluded from `/health/live`; no reachability claims; no other-dependency health changes (resolved C2/VI + Plan Challenge L1).
- **Verification / review**: Q11 closing bar; Size L Sentry/Ledger/Compass on the PR; two remediation rounds max. Plan Challenge round cap remains two; Quill does not start another challenge round.

## Project Structure

Proposed new paths stay inside existing folders and are marked *(proposed)*. Exact existing DI/mapping filenames are Phase B Wisp facts (Q10) — do not guess rewrites today.

```text
specs/DEV-453/
  brief.md, CONCLUSIONS.md, ASSUMPTIONS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  checklists/requirements.md
  runtime-acceptance.md                 (Phase B receipts; never prefilled PASS)

src/LamuFlix.Core/
  Ports/ISubtitleProvider.cs            *(proposed)*
  Ports/ISubtitleSidecarStore.cs        *(proposed)*
  (subtitle sealed command/query records + sealed handlers + FluentValidation validators follow existing Core feature-folder conventions — exact folders at Build within existing Core layout; no new top-level project/layer)

src/LamuFlix.Infrastructure/
  (OpenSubtitles HTTP/hash adapter — *(proposed)* under existing Infrastructure layout)
  FileSystem/                           (contained sidecar adapter — *(proposed)* beside existing FS adapters)

src/LamuFlix.Api/
  Endpoints/SubtitleEndpoints.cs        *(proposed)* thin TypedResults prepare + serve (raw {id} capture/parse)
  (existing DI registration + endpoint-mapping entrypoints — exact filenames from Phase B Wisp T001; do not assert ApiEndpoints.cs)
  (ServiceDefaults/Infrastructure-owned telemetry + always-Healthy optional-provider local health wiring — resolved C2/VI; zero network/quota; ready|disabled_or_unconfigured metadata; excluded from /health/live)

CONTEXT.md                              (repository root; append-only five settled subtitle terms — resolved C4/VIII; same spec PR)

web/src/api/openapi.json                (generated — tooling only)
web/src/api/types.ts                    (generated — tooling only)

tests/LamuFlix.UnitTests/               (focused new tests; exact filenames at Build)
tests/LamuFlix.IntegrationTests/        (focused new tests + OpenAPI inventories as applicable)
tests/LamuFlix.ArchitectureTests/
  ArchitectureTests.cs                  (append-only whitelist for the two new ports; atomic with ports)

docs/adr/0038-subtitle-provider-and-sidecar-boundary.md   (Keel-authored; Proposed; Quill does not edit)
.specify/memory/constitution.md         (Keel-owned named-port enumeration for the two ports — Quill does not edit)
```

**Layout decisions**:

- No research/data-model/contracts artifacts — decisions frozen in brief Q3–Q10.
- Quill owns spec/plan/tasks/receipt/checklist and the authorized append-only root `CONTEXT.md` five-term compliance edit. Quill does not edit brief/CONCLUSIONS/ASSUMPTIONS/ADRs/constitution/production code.
- Exact new test filenames and exact Infrastructure subfolder names under existing projects are chosen at Build without expanding scope (Q7, Q10).
- Exact existing DI/endpoint mapping filenames deferred to Phase B Wisp (Q10).
- Options nesting: `DownloadAllowedHosts` / `MaxDownloadBytes` / `RequestTimeout` under vendor-mandated config path `Subtitles:OpenSubtitles` (brief-authorized nesting). Implementation C# types are functionality-named `SubtitlesOptions` / nested `SubtitleProviderOptions` (and functional telemetry identifiers); the verbatim owner checkbox still says `SubtitlesOptions / OpenSubtitlesOptions` and stays unchecked.
- Hash/provider seam: moviehash computed through authorized read-only `ILibraryFileReader`; provider port is data-only; no path/stream into the OpenSubtitles adapter.
- Accepted personal-scale risks (recorded, not expanded): concurrent pre-create double download before atomic-create winner; crash-orphan temp between temp create and atomic move. No locking/janitor scope.

## Generated-contract workflow

1. Opt-in export: set `LAMUFLIX_REGENERATE_OPENAPI=true`, run OpenAPI regeneration test (writes committed `web/src/api/openapi.json` and Verify snapshot together), unset the env var.
2. From `web/`: `npm run generate:api` produces generated TypeScript (`web/src/api/types.ts`) via existing tooling.
3. `npm run check:api` (or equivalent) verifies drift.
4. Never hand-edit generated artifacts.

## Test Strategy

| Surface | Coverage |
|---|---|
| Selection / hash | Preference order, ties, noMatch; moviehash vectors/boundaries (≥131,072 eligibility) via `ILibraryFileReader`; title fallback from confirmed Movie primary-title only when authorized; blank title → noMatch |
| Provider adapter | WireMock.NET auth/headers, search/download, pre-request HTTPS/host checks, `AllowAutoRedirect=false`, all-`3xx`→502 with never-contacted proof, quota 429 + Retry-After (delta-seconds and HTTP-date), provider timeout vs caller abort, error mapping, minimal downloaded-SRT criteria; no real credentials |
| Sidecar store | Real temp FS: contained read; atomic create-without-replace; no overwrite; concurrent create → alreadyPresent; ordinary cancellation cleanup; traversal/reparse/nonregular denial; accepted double-download / crash-orphan risks recorded (no lock/janitor) |
| Prepare/serve API | alreadyPresent before acquisition checks; saved with always-matched `selectedLanguage` / noMatch null; raw-id malformed **400** + well-formed absent **404** + application-validation **422** + 403/404/409/429/502/504 (never claim all validation is 400; never remap 422→400; no invented GET 422); GET bounded bytes + nosniff/attachment headers independent of acquisition settings; LocalPlay independence; no stream-triggered provider calls |
| Provider safety | non-SRT/invalid/oversized/blocked-prefix/missing-timestamp rejection; invalid local BaseUrl→409 vs upstream unsafe/redirect→502; no-retry proof; no hop cap / numeric defaults invented |
| Observability | Functionality-named TelemetryConstants + movie-id structured events; ServiceDefaults-owned wiring; always-Healthy optional-provider local health with ready/disabled_or_unconfigured metadata; enabled/disabled/configured/invalid matrix; zero provider calls; excluded from `/health/live` |
| Contracts | OpenAPI/TS regeneration + inventory/drift checks |
| Runtime | Configured-key download; writable save; BS Player exact-basename autodetection; read-only serve — or `Could not run` |
| Delivery gates | Roslyn; complexity; InspectCode; mutation on eligible changed C#; property; vulnerable packages; format; `dotnet test`; applicable web/contract checks — honest exits only |

No Gherkin. Phase A runs none of these and claims no gate result.

## Ordering (brief Q9)

1. Gate 1 merge receipt.
2. Phase B Wisp pickup facts (dependencies, DI/mapping filenames, hash vectors, allowlist, provider schema, whitelist plan).
3. Keel pickup analyze + Principle I constitution/governance for the two ports (or exact-equivalent evidence).
4. Atomic ports + whitelist code + DTO/options surfaces (demo startup safe; C4/VII ValidateOnStart departure remains blocked until owner checkbox + scoped constitution amendment; Principle I independent).
5. Hash/selection tests → provider adapter (include content/redirect/no-retry proofs).
6. Sidecar store tests → sealed handlers + thin TypedResults endpoints + resolved C2/VI always-Healthy local health/telemetry; existing-track serve independent of acquisition settings; C1/V 400-vs-422 OpenAPI/tests.
7. Contract regeneration + demo/writable docs.
8. Applicable harness gates.
9. Live runtime receipts (or `Could not run`).
10. Size L three-axis review on PR → delivery under Conductor.

## Risks

- **Dependency not delivered**: DEV-457 / DEV-454 contracts must be delivered before affected implementation paths (Q9).
- **Principle I incomplete**: ports without constitution/governance/feature-folder compliance + whitelist fail architecture gates — modeled as Keel/Build prerequisites (Q7; Plan Challenge item 5.2).
- **Hash/allowlist/protocol drift**: Phase B must verify vectors, exact API/download hosts, protocol compatibility, and `ILibraryFileReader` head/tail contract before implementing those paths; STOP if redirects are required (Q8–Q9; Plan Challenge item 1).
- **Secret leakage**: ApiKey secret-only; never log keys/paths/content; no committed credentials (Q8, Q10).
- **Quota burn**: no stream-triggered downloads; no automatic retries; existing-sidecar short-circuit (Q3, Q5–Q6).
- **SSRF / partial writes**: pre-request HTTPS/host validation + redirects disabled + all-`3xx`→502 + atomic create-without-replace + ordinary cancellation cleanup (Q8; Plan Challenge item 1).
- **Accepted nonblocking (personal-scale)**: concurrent prepares may download twice before atomic-create winner resolves loser to `alreadyPresent`; abrupt process death may orphan a temp file outside ordinary cancellation cleanup. Recorded in T009/T012; **no** per-movie locking or background temp-janitor (Plan Challenge item 2.3 / Sentry F6).
- **Optional-provider health limitation**: always-Healthy local readiness metadata does not prove upstream reachability; excluded from `/health/live` (Plan Challenge item 7 / Ledger L1).
- **Scope creep into UI / LocalPlay / packages**: frozen Q3–Q10; follow-ups only (Q5, Q7, Q11).

## Loop discipline (Q11)

- Closing bar: High/Medium block; security/contract/ticket failures block regardless of label; every Low needs explicit cited disposition.
- Size L: Sentry, Ledger, and Compass reports published on the PR pinned to the exact reviewed head.
- Frozen scope: Q3–Q10 backend prepare/serve, ordered preferences, exact-basename SRT, safe writable acquisition, read-only existing tracks, generated contracts/config/tests/docs; anything else is a follow-up issue.
- Two review/remediation rounds maximum; unresolved blockers → Bernstein; never automatic clearance.

## ADR status

ADR **0038** (`docs/adr/0038-subtitle-provider-and-sidecar-boundary.md`) is Keel-authored and **Proposed**. Quill does not write or edit ADRs. Acceptance lifecycle remains outside this drafting ask.
