# Implementation Plan: Multi-provider subtitle acquisition seam

**Branch**: `feature/551-spec` (worktree `F:/Dev/LamuFlix.worktrees/feature-551-spec`) | **Date**: 2026-11-28 | **Spec**: `specs/DEV-551/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q12); `CONCLUSIONS.md`; `ASSUMPTIONS.md` (Q6); Proposed ADR `docs/adr/0052-multi-provider-subtitle-acquisition.md`; Maestri notes `DEV-551`, `recon-DEV-551`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: Draft (plan-challenge fix round applied) — Quill drafting only. Plan challenge dispositions recorded; owner Gate 1, Build, and runtime acceptance remain pending. Quill does not declare PLAN FROZEN.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `contracts/search-response.md`, `checklists/requirements.md`, `checklists/multi-provider-subtitles.md`, `DRAFTING_RECEIPT.md`. No `research.md` / `data-model.md` / `quickstart.md` — grill Q1–Q12 freeze inventable unknowns; no schema change; additive API contract under `contracts/`.

## Summary

Deliver ADR 0052 amending ADR 0041 for simultaneous multi-provider movie subtitle search on the existing seam: named typed-client registration catalog, per-provider validation and outbound destination/credential isolation, concurrent aggregate search with additive provider outcomes, provider-bound Candidate IDs and explicit downloads, shared WireMock adapter contract suite with OpenSubtitles as sole production adapter, per-provider health observations, glossary/setup-doc updates, and regenerated OpenAPI/TypeScript — preserving legacy request contract, single-OpenSubtitles behaviour, existing OpenSubtitles assertions, and all web tests under the Q11 closing bar.

## Technical Context

**Language/Version**: C# / .NET 10 (`LamuFlix.Core`, `LamuFlix.Infrastructure`, `LamuFlix.Api`) + Vite/React/TypeScript generated contracts under `web/src/api/` per `specs/PRODUCT.md` §1. UI none.

**Primary Dependencies**: Existing `Microsoft.Extensions.Http` typed clients, WireMock.Net in IntegrationTests, xUnit v3, FsCheck property conventions, existing OpenAPI → TypeScript generate:api chain. No new NuGet/npm dependency (`brief.md` Q8; `PRODUCT.md` §5 item 1).

**Storage**: No database or migration change. Process-local Candidate ID store retains existing 1000-entry / five-minute defaults (Q7).

**Testing**: Shared abstract WireMock suite through real typed HttpClients; IntegrationTests for composition/API/health; UnitTests for handlers/binder/properties; preserve `OpenSubtitlesSubtitleProviderTests` and all existing web tests; `SubtitleSetupDocumentationTests`; ArchitectureTests with full suite. Never mock the data-access driver for query-translation behaviour (N/A for HTTP provider path).

**Target Platform**: Local API host; Compose/home-cluster unchanged except typed subtitle settings documentation.

**Project Type**: Existing ports-and-adapters .NET solution; Core port/features, Infrastructure adapters, Api endpoints/DTOs, tests, docs, generated web contracts.

**Performance Goals**: Aggregate latency bounded by the slowest per-provider timeout (Q1); no invented SLA beyond that bound.

**Constraints**: Charter §2.3 care-list items deliberately ruled (Q1–Q12). No LocalPlay/secrets hardcoding/`Process.Start`. Serialize with DEV-550 on `SubtitleProviderServiceCollectionExtensions.cs` if both land. Honest gate labels; never lower thresholds; never merge. Size L requires Risk (Sentry), Standards (Ledger), Spec (Compass). Closing bar and caps: `brief.md` Q11–Q12.

**Scale/Scope**: Size L; multi-provider seam + shared contract suite + additive API + health + docs. Parent chain via ticket; ADR 0052 claimed.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No structural owner checkbox (`PRODUCT.md` §5): Patron Q1–Q12; no ticket contradiction; no constitution departure. Ordinary owner Gate 1 remains the Spec Kit PR merge path. ADR 0052 acceptance is first in the delivery task sequence after Gate 1 and retains the before-deliverable-merge bar; it is not a Gate 1 substitute (Q11; brief.md:19).

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation; ArchitectureTests | **PASS** — keep existing `ISubtitleProvider`; extend port/search/candidate types with provider identity, capabilities, opaque file reference, outcomes; one ordered concrete provider **lookup on the Core side** of that seam (not a new port/layer) owning host-lifetime per-provider spacing and observation state; typed HTTP client registration/composition stay **outside** Core. ArchitectureTests remain required green (Ledger S4). |
| II (Explicit Handlers) | Handlers + AddHandler | **PASS** — adapt existing Search/Download/Lookup handlers; no new mediator/repository layer (Q8). |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — no Movie mutation. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — additive ProblemDetails `providers` extension on **settled search** errors only; download keeps existing ProblemDetails without `providers`; preserve Principle V 422/400 split and **single centralized handler** with `traceId`; closed safe reason codes as Core Enumerations with JSON Name conversion in approved DTO scope (Q3; S2/S3/M4). |
| VI (Observability) | Safe diagnostics | **PASS** — safe provider codes only; unsafe-URL startup warning carries provider name + `unsafe_base_url` only; never log credentials, raw URLs, query, or upstream message text (Q3–Q4; M2). |
| VII (Configuration / LocalPlay / Process.Start) | Options; secrets isolation; LocalPlay gated | **PASS** — typed per-adapter options; OpenSubtitles section path preserved; Constitution VII **subtitle acquisition ValidateOnStart exception** is explicit (provider-local off/unusable for invalid timing, missing credentials, or unsafe URL — never host abort); **search** 409 when no enabled provider is usable (all-enabled check; usable sibling leaves search configured); **download** 409 when the **bound** provider is missing/invalid/unusable **after** ADR 0041 read-only precedence, even if a sibling remains usable, with zero paid calls to either provider; credentials in existing typed secret configuration; no LocalPlay or `Process.Start` (Q2, Q4, Q8; Ledger S1; ADR 0052; M3). |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — add Subtitle Provider; keep existing Subtitle Candidate / Candidate ID / Local Subtitle Track / Subtitle Match Percent (brief Approach). |
| IX (Test Pyramid) | Meaningful layers; WireMock; WebApplicationFactory; property tests | **PASS** — shared WireMock suite via real typed clients; IntegrationTests API/composition/health; UnitTests binder/handler/property; web suite preserved. |

### Stack / API / workflow checks

| Check | Plan response |
|---|---|
| Tech stack / API docs | **PASS** — additive public DTO fields only (Q3); regenerate openapi.json → types.ts; no new route or request parameter (Q1). |
| Static-analysis gates | Every changed `.cs` task completes only after Roslyn, cyclomatic complexity, and InspectCode pass on those files; ArchitectureTests green; full delivery matrix per `brief.md` Gate expectations / Q11. Keep `SKIPPED`, `N/A`, and `Could not run` distinct from PASS. |
| Web gates | Lint/test/build + OpenAPI/TypeScript drift; all existing web tests green (ui:none; no new SPA work). |
| Mutation | Applicable production C# under `src/` expects mutation under harness policy; record PASSED / FAILED / SKIPPED (scope-empty) / N/A honestly. Honour static-initialization discipline. |

### Complexity Tracking

No constitution violations requiring justification. Core-side ordered provider lookup is a concrete collaborator on the existing seam; typed HTTP client registration stays outside Core; no new project/package/layer/port.

## Approach

- **Spec Kit drafting → Gate 1**: Spec Kit artifacts drafted; Keel analyze / plan challenge follow the pipeline; Spec PR opened under ordinary Gate 1; Build starts after Gate 1 merge receipt, pickup clearance, and DEV-525 Done verification. Quill does not claim freeze or Gate 1.
- **ADR**: Keel-owned ADR 0052 remains Proposed through Spec PR as required. After separate Gate 1, **Accept ADR 0052 first in the delivery task sequence** (T030 before pickup/Build); retain the Q11 before-deliverable-merge bar (Accepted required before any deliverable merges). Leave ADR 0041 intact; precedence documented in 0052 (Q10; brief.md:19).
- **Pickup**: Fresh read-only recon against pickup HEAD for authorized paths in brief Planned file scope (`ISubtitleProvider`, search/download/Lookup/binder, options/HandlerRegistration, OpenSubtitles adapter + request URI policy + health, SubtitleEndpoints/DTOs, subtitle tests, setup docs, openapi/types, CONTEXT.md, ADR 0052). Confirm DEV-525 Done **before Build**. Recheck serialization with DEV-550 on `SubtitleProviderServiceCollectionExtensions.cs`. Stop `needs recon:` if a required path/fact is missing. Stop for cited Patron ruling before any outside-envelope or §2.3 protected edit.
- **Task order (`brief.md` Approach and planning constraints)**:
  1. Plan challenge and owner Gate 1 as separate gates (Spec PR may carry Proposed).
  2. **ADR acceptance first in the delivery task sequence after Gate 1** (T030); before-deliverable-merge bar retained (Q11).
  3. Verify DEV-525 Done before Build (T001).
  4. Typed port extensions + Core-side ordered provider lookup (host-lifetime spacing/observation state) + per-adapter validation under ValidateOnStart exception + outbound request policy (dual HTTP opt-in; response-independent destinations; redacted warnings) + OpenSubtitles adapter adapt + shared typed-client contract suite green **before** fan-out and downloads; typed HTTP client registration/composition stay outside Core.
  5. Wire ranked batch binding **before** the search path issues Candidate IDs or claims parity; then route by server-bound identity across search, download, and SubtitleLookup; reuse existing publication safeguards, library policy, and **centralized** error handling (Principle V single handler).
  6. Prove OpenSubtitles adapter contract (T018) and endpoint parity plus two-provider isolation with complete wire assertions (T010: request-level branch, simultaneous-fault first-match, Q2-409 `providers` presence/order, request-level `providers` absence) **before** generating API artifacts; T026 depends on both completed receipts and those wire assertions (**search-only** generation proof).
  7. Map C# DTOs (Patron S5 closed `state`/`reason` as Core Enumeration or equivalent with JSON `Name` conversion in approved DTO scope) and settled **search** ProblemDetails extension key `providers` to OpenAPI and generated TypeScript with wire-level and generated-schema coverage; preserve all existing web tests; explicitly run `SubtitleSetupDocumentationTests`.
  8. Wire local health to Core-side settled observation state shared with search; update setup documentation with new settings (incl. dual HTTP opt-in), valid ranges, and provisioning order.
  9. Size L ship-review split and rebased-head gate verification at exact reviewed head (brief Gate expectations).
- **Do not**: add real adapters beyond OpenSubtitles; build picker/track labels/auto-download/TV/scorer edits; add NuGet/npm/packages/projects/layers/schema; hardcode secrets; touch LocalPlay/`Process.Start`; invent numeric safety defaults for timeout/gap; lower gate thresholds; claim Gate 1 / plan freeze / delivery PASS from this draft.

### Patron-approved identifiers (Ledger S5; see `CONCLUSIONS.md` / `ASSUMPTIONS.md` / `DRAFTING_RECEIPT.md`)

| Item | Name | Basis |
|---|---|---|
| Provider order config | `Subtitles:ProviderOrder` | Q8 |
| Adapter enablement | per-adapter `Enabled` | Q8; Patron S5 |
| Call spacing | per-adapter `MinimumRequestGap` | Q8; Patron S5 (TimeSpan; valid `>= TimeSpan.Zero`; no positive safety default invented) |
| Provider timeout | existing `RequestTimeout` (`> TimeSpan.Zero`) | Q8; existing validator |
| OpenSubtitles section | `Subtitles:OpenSubtitles` | Q8; preserved |
| Outcome states | `off`, `unusable`, `ok`, `quota`, `timeout`, `failed` | Patron S5; Q2–Q5; `contracts/search-response.md` |
| Reason codes | closed set in `contracts/search-response.md` (`off`, `unsafe_base_url`, `provider_not_configured`, `ok`, `empty`, `search_rate_limited`, `provider_timeout`, `provider_unavailable`, `no_search_input`) | Patron S5; Q3 |
| Core-side provider lookup | ordered concrete lookup on Core side of existing `ISubtitleProvider` seam; owns host-lifetime per-provider spacing and observation state; shared by search/download/Lookup/health | Q8; Constitution I; Ledger S4 (not a new port/layer) |
| Typed-client composition | named typed HTTP client registration outside Core (`SubtitleProviderServiceCollectionExtensions` + `HandlerRegistration`) | Q8; Constitution I |
| ProblemDetails extension key | `providers` (settled **search** errors only) | Q3; plan-challenge M4 |

## Verification Approach

- **Shared contract suite**: WireMock through real typed HttpClient — every declared search kind; in-cap SRT download; oversize/non-SRT rejection; HTTP 429; timeout; 5xx; cross-host redirect refusal; HTTPS downgrade refusal; credentials restricted to HTTPS search origin; dual opt-in plain HTTP under two conditions proven as **four independent typed-client gates**: (1) permit credential-free declaration **plus** explicit opt-in, (2) refuse missing opt-in, (3) refuse missing credential-free declaration despite opt-in, (4) refuse credential/login configuration despite both opt-ins; OpenSubtitles HTTPS-only; response-independent destination trust with refusal of disallowed initial URL and redirect **before** any contact; unsafe base URL startup refusal with redacted warning (provider name + `unsafe_base_url` only) and host/local-track continuity (`brief.md` Test strategy; H1/M1/M2). OpenSubtitles adapter adapt + green typed-client suite proof sit in the foundation path.
- **Fan-out / isolation**: Two stand-ins — concurrent call for one language; deterministic provider order; 503/quota/timeout affect only origin; caller cancellation unchanged; capability priority; missing earlier identifier with later available input (skip, not terminal); no formable input at all; empty-only fallback; stop-on-timeout/quota/failure; spacing; search 429 vs known download exhaustion on otherwise valid candidates.
- **Binding**: Ranked batch binding before search issues Candidate IDs; every returned over-capacity token resolves immediately; earlier token evicted by a later search takes existing stale response with zero paid calls (US2 scenarios 4–6 covered independently of expiry); B Candidate ID invokes B only; A/B reference-string collision isolated; cross-provider/cross-movie misuse cannot issue paid call; bounded ranked issuance across providers and preferred languages at full capacity; explicit property coverage for provider-bound bounded Candidate ID invariants; injected time and deterministic randomness.
- **API search (T010 endpoint parity + isolation)**: OpenSubtitles-only legacy parity including each legacy quota/availability projection and any baseline **non-200** settled legacy outcome against unchanged baseline (do not invent status); additive fields; exclusive ordered provider `state`/`reason` under pre-I/O precedence (disabled / unsafe URL / missing-invalid credentials-timeout-gap) including **simultaneous-fault first-match** cases plus request-time reasons; usability freeze at request-time acquisition config validation (timeout/quota/failure/no-formable-input cannot yield Q2 409 when ≥1 provider was usable); ProblemDetails `providers` **present** with ordered classified rows on Q2 search 409 and on legacy settled errors, **absent** on request-level errors; disjoint status branches — **request-level existing** (`providers` absent); one usable + off/refused → 200; all unusable → **search** 409 with zero paid calls (`providers` present); multi-usable zero-candidate 200; missing required key → zero requests. Download 409 and read-only precedence are **not** assigned here.
- **API download (T015 endpoint integration)**: **Bound-provider** acquisition **409** when the bound provider is missing/invalid/unusable **even with a usable sibling**, with zero paid calls to either provider; ADR 0041 read-only precedence **before** acquisition validation; legacy HTTP status and ProblemDetails field assertions **without `providers`** on acquisition 409, read-only, and stale/invalid paths (wire assertions); plus US2 Q7 proofs (see Binding) (Ledger S1; ADR 0052; M4).
- **Invalid timing / ValidateOnStart (T020/T021)**: Invalid timing on one provider leaves host and existing tracks available, classifies that provider off, sends zero calls to it, and allows usable-sibling search 200 (M3/S1).
- **Regression**: Preserve and run `OpenSubtitlesSubtitleProviderTests` and all existing web tests; **search** 409 (no enabled provider usable) regression in T010; **download** bound-provider 409 / read-only-precedence / stale-invalid / unchanged ProblemDetails-without-`providers` regression in T015.
- **Health**: unobserved/reset; safe last settlement; cancellation exclusion; zero health-network calls; readiness continuity; Core-side observation state shared with search for host lifetime (S4).
- **Docs/contracts**: Explicitly run `SubtitleSetupDocumentationTests` (incl. dual HTTP opt-in and Patron S5 key names); OpenAPI/TypeScript drift; wire-level and generated-schema coverage after T018 adapter-contract and T010 endpoint-parity/isolation receipts; T026 remains **search-only**.
- **Gates / closing bar (`brief.md` Gate expectations / Q11–Q12)**: Exact-head Roslyn; cyclomatic complexity at build/refactor thresholds; InspectCode; property tests; vulnerable packages; format; full .NET suite; mutation when applicable; web lint/test/build with drift; ArchitectureTests with full suite; honest SKIPPED/N/A/Could not run. Size L three review axes, ship-review split, and rebased-head gate verification run only after **all** in-scope story/proof receipts including T015 download integration, T024 health, and T028 setup documentation; two plan-challenge rounds and two implementation review rounds max; at most two fix commits per implementation round. ADR 0052 Accepted first in delivery task sequence after Gate 1, with before-deliverable-merge bar retained. Owner merges.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-551/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md              # This file (mirrored to private IMPL_PLAN)
├── tasks.md             # Mirrored to private TASKS
├── contracts/
│   └── search-response.md
├── DRAFTING_RECEIPT.md
└── checklists/
    ├── requirements.md
    └── multi-provider-subtitles.md
```

### Source Code (authorized responsibility envelope — brief Planned file scope)

```text
docs/adr/0052-multi-provider-subtitle-acquisition.md   # Keel-owned; Accept before deliverable merge
CONTEXT.md                                             # Add Subtitle Provider

src/LamuFlix.Core/Ports/                               # ISubtitleProvider, SubtitleSearch, SubtitleCandidate (+ identity, capabilities, opaque ref, outcomes)
src/LamuFlix.Core/Features/Subtitles/                  # Search/Download/Lookup/Binder + Core-side ordered provider lookup
                                                       # (host-lifetime per-provider spacing + observation state; Core Enumerations)
src/LamuFlix.Core/Options/                             # Typed subtitle provider settings (+ ProviderOrder, Enabled, MinimumRequestGap; S5)
src/LamuFlix.Api/HandlerRegistration.cs                # Typed-client composition wiring (outside Core); central ProblemDetails handler touch

src/LamuFlix.Infrastructure/Adapters/                  # OpenSubtitlesSubtitleProvider, SubtitleProviderServiceCollectionExtensions
                                                       # (typed HTTP client registration/composition), SubtitleRequestUriPolicy,
                                                       # SubtitleProviderHealthCheck

src/LamuFlix.Api/Endpoints/                            # SubtitleSearchResponse + SubtitleEndpoints additive contract

tests/LamuFlix.IntegrationTests/                       # Shared contract suite, composition, API regression (search+download), health
tests/LamuFlix.UnitTests/                              # Handler/binder/property/setup-doc surfaces (existing homes)

README.md (subtitle setup sections)                    # Named settings, ranges, provisioning; key/placeholder examples only
web/src/api/openapi.json
web/src/api/types.ts
```

**Structure Decision**: Extend the existing subtitle provider seam inside Core / Infrastructure / Api. No new project, package, or port. Shared ordered provider lookup lives on the **Core** side of the existing seam and owns host-lifetime per-provider spacing and observation state; typed HTTP client registration and composition stay **outside** Core; closed outcome types use the existing Enumeration pattern at the Core subtitle outcome boundary with JSON Name conversion in the approved API DTO scope; Principle V single error handler remains the sole ProblemDetails carrier (S2/S3/S4).

## Named identifiers

See Patron-approved identifiers table above and `contracts/search-response.md`. OpenSubtitles registered name remains the stable configured provider id for the sole production adapter.
