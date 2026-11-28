# Feature Specification: OMDb and TMDb metadata-provider contract suite

**Feature Branch**: `feature/557-spec`

**Created**: 2026-11-28

**Status**: gate1: provisional

**Input**: Ticket DEV-557 (size M; no `ui` tag); grill-closed `brief.md` (Q1–Q7); `CONCLUSIONS.md`; Maestri notes `DEV-557`, `recon-DEV-557`; `specs/PRODUCT.md`. Decisions come only from brief and its rulings. Facts for per-adapter 401 outcomes come from `recon-DEV-557` (verified 401 follow-up). Quill invents no decisions.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Charter §2.3 / Q6; `PRODUCT.md` §5). Ordinary owner Gate 1 remains the Spec Kit PR merge path.

## Clarifications

### Session 2026-11-28

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Shared harness with DEV-551 and Phase B block (Q1), no chain run (Q2), 401 OMDb-wins with file:line citation (Q3), twin vs residual retention (Q4), Stryker/line-count measure (Q5), §2.3 none (Q6), InspectCode on `SecondaryMetadataProviderTelemetryTests.cs` (Q7) are settled in `brief.md` and `CONCLUSIONS.md`. No taste assumptions (`ASSUMPTIONS.md` absent). No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour. Any gap Quill cannot close from brief/CONCLUSIONS goes to `DRAFTING_RECEIPT.md` as `needs decision:`. Conductor ask skipped interactive clarify and checklist commands.

## What the User Will See *(mandatory)*

Nothing visible: this ticket has no `ui` tag. It consolidates metadata-provider adapter conformance into one shared contract suite and removes duplicated twin cases from the existing OMDb and TMDb integration-test families. End-user library, enrichment UI, and SPA behaviour are unchanged.

## Scope

**In scope** (`brief.md` Frozen scope / Plan decisions / Closing bar; Q1–Q7; ticket *Wanted* 1–4, *Done when*, *Where it lands*):

1. **Abstract metadata-provider contract suite**: One abstract xUnit contract class that each metadata adapter runs through the shipped DI composition (`MetadataProviderServiceCollectionExtensions.cs:43-59`): real typed `HttpClient`, real resilience pipeline and quota handler, against WireMock. Cases (*Wanted* 1) are the **minimum**, not the ceiling: found by IMDb id; found by title and year; not found; 401 reported as a key problem; 429 as `RateLimited`; 5xx as `ProviderUnavailable`; timeout; unreachable server; `Retry-After` inside and past the budget; breaker opening; cancellation; telemetry (one span and one duration per call; no key, title or IMDb id in any tag). The suite MUST also cover every twin in the inventory; known extra beyond *Wanted* 1: resilience **"transient server error then success"** (recovers rather than ending in `ProviderUnavailable`) (`brief.md` Contract case set).
2. **Both adapters run it**: OMDb and TMDb each run the suite through thin derived fixture classes (`brief.md` Frozen scope; *Wanted* 2).
3. **Twin removal with residuals**: Twin cases present in both families across lookup, resilience and telemetry leave the adapter classes and live only in the suite. A twin with no matching *Wanted* 1 case gets a contract case added — it is never kept as a residual (residuals are only adapter-specific assertions inside a twin; Q4). Single-adapter cases stay: TMDb cancel-during-request, OMDb request-limit-body 401 (quota), `MetadataProviderTmdbSignalTests`, `SecondaryMetadataProviderTrailerTests`, `SecondaryMetadataProviderMappingTests`, and the composition and chain-composition tests. Where a twin carries an adapter-specific assertion the contract cannot express, only that residual assertion stays in its adapter class (Q4).
4. **Shared harness with DEV-551 (Q1)**: Consume the harness DEV-551 lands; define no rival harness. This spec names only contract cases and adapter fixtures — never harness type names. Phase B pickup is blocked until the DEV-551 implementation merges to main; Phase B drift check re-runs `/speckit-analyze` against main, re-verifies harness type names there, and repins `plan.md` and `tasks.md` to them. If the merged harness is subtitle-bound, extracting its provider-agnostic part (WireMock server lifetime, DI host build, telemetry capture) into a base both suites use is in scope (*Wanted* 4 forced edit); DEV-551 subtitle assertions are not touched.
5. **401 outcomes recorded (Q3)**: See **Current 401 outcomes (recon)** below. Ordinary 401 behaviour already matches across adapters; the suite asserts that OMDb ordinary-401 key-problem outcome. OMDb separately classifies request-limit-body 401 as quota failure (OMDb-only). TMDb production code is brought to OMDb behaviour only if "reported as a key problem" still requires a change after the suite asserts OMDb ordinary 401; the PR body states any difference.
6. **Measurement (Q5)**: Stryker on `OmdbMetadataProvider.cs` and `TmdbMetadataProvider.cs` with identical config, before on the Phase B base and after on the head; score must not drop per file; receipts recorded. Line-count **before** is the T002 measurement on the Phase B base (5,072 at `e1649d53` is historical context only). After covers the 12 recon files plus every new DEV-557 contract and fixture file and any harness lines DEV-557 adds; lines DEV-551 merged are excluded; after must be below before (P10).
7. **Gates and InspectCode (Q7)**: Roslyn, cyclomatic complexity (≤ 15; refactor gate 6), InspectCode on changed files; `dotnet test` green; `dotnet format --verify-no-changes`. Baseline InspectCode: 2 warnings (`MethodHasAsyncOverloadWithCancellation` at `SecondaryMetadataProviderTelemetryTests.cs:98,102`). If that file is in the diff at all, fix both warnings; otherwise leave as `noted, no ticket`.

**Current 401 outcomes (recon)** — required by Q3 / brief Spec prerequisite (`recon-DEV-557` verified 401 follow-up, HEAD `a0dc21f2249d6f5e98c7b84a8db4a1125361a68b`):

| Adapter | Source outcome | Existing lookup assertion |
|---|---|---|
| OMDb | `OmdbMetadataProvider.cs:408-410` — HTTP 401 for bad key and spent daily quota; body distinguishes quota. `:414` — request-limit-body 401 handled as quota (not unauthorized). Ordinary unauthorized reaches `StatusFailure` at `:431-433`, then `UnauthorizedFailure` at `:452-455`: logs `InvalidApiKey`, records `InvalidResponse` with `wasUnauthorized: true`, returns `Failed(InvalidResponse)`. | `MetadataProviderLookupTests.cs:459-474` — stubs 401; asserts `InvalidResponse`, one request, OMDb API-key configuration advice. |
| TMDb | `TmdbMetadataProvider.cs:507-509` — 401 → `UnauthorizedFailure`; `:537-540` — logs `InvalidApiKey`, records `InvalidResponse` with `wasUnauthorized: true`, returns `Failed(InvalidResponse)`. No OMDb-style quota-body exception in this file. | `SecondaryMetadataProviderLookupTests.cs:215-229` — stubs 401; asserts `InvalidResponse`, one request, unhealthy health status, TMDb API-key advice. |

Ordinary 401 behaviour matches on the shared assertion (Failed `InvalidResponse`, one request, `wasUnauthorized`). Advice text differs per adapter and is fixture-hook supplied; only the TMDb lookup test asserts unhealthy. OMDb alone classifies request-limit-body 401 as quota failure. Suite asserts the OMDb ordinary-401 key-problem outcome for the shared 401 case.

**Out of scope** (`brief.md` Frozen scope; ticket *Out of scope*; Q2):

- A new metadata provider.
- Changing how `ChainedMetadataProvider` picks a provider.
- Running the contract suite against `ChainedMetadataProvider` (Q2 default: no).
- New NuGet/npm dependency; new project, top-level folder, or architectural layer.
- Database schema; public API / OpenAPI / TypeScript shape.
- `Features:LocalPlay`, secrets hardcoding, or `Process.Start`.
- Changing DEV-551 subtitle assertions.
- Production adapter edits outside the Q3 401-alignment carve-out.
- Edits outside the 12 recon family files except the Q1 harness clause and Q3 carve-out, without a new Patron ruling.
- Anything else is a follow-up issue, not a finding in this round (Critical/High/broken behaviour only may become follow-up tickets; otherwise `noted, no ticket`).

**Precedence**: Ticket *Wanted* / *Done when* / *Out of scope* / *Where it lands* are approved authority (`PRODUCT.md` §3). Brief Q1–Q7 and `CONCLUSIONS.md` govern plan boundaries. Quill invents no decisions.

**Dependencies**: Run after DEV-551 (*Needs*). Phase B / Build pickup blocked until DEV-551 implementation merges to main (Q1).

## Domain vocabulary *(mandatory — spec-local; no CONTEXT.md expansion)*

**Metadata-provider contract suite**
One shared abstract xUnit suite that states what every `IMetadataProvider` adapter must do for the ticket Wanted cases, executed through shipped DI against WireMock.
_Avoid_: a second rival harness beside DEV-551; running the suite against the chain

**Twin case**
A case present in both OMDb (`MetadataProvider*`) and TMDb (`SecondaryMetadataProvider*`) lookup, resilience, or telemetry families with the same or nearly the same meaning; after this ticket it exists only in the contract suite (residuals for adapter-specific assertions may remain).
_Avoid_: deleting single-adapter-only cases; deleting composition / chain / signal / trailer / mapping suites wholesale

**Ordinary 401 key-problem outcome**
Shared contract assertion: Failed `InvalidResponse` after one request, with `wasUnauthorized` recording. Adapter-specific key advice text (and any health assertion) is supplied by the fixture hook — advice wording differs per adapter, and only the existing TMDb lookup test asserts unhealthy (recon).
_Avoid_: treating OMDb request-limit-body 401 (quota) as the shared key-problem case; baking one adapter's advice string into the shared assertion

Consumed vocabulary (already in use; not redefined): `IMetadataProvider`, `MetadataLookup`, `MetadataLookupResult`, `EnrichmentFailureCategory`, `ChainedMetadataProvider`, WireMock, typed `HttpClient`, resilience pipeline, quota handler.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Shared contract suite for both adapters (Priority: P1) 🎯 MVP

A maintainer runs one abstract contract suite against OMDb and against TMDb through the shipped DI composition. Every Wanted case passes for both adapters, including ordinary 401 as a key problem under the OMDb outcome recorded above, without inventing harness type names before DEV-551 merges.

**Why this priority**: *Wanted* 1–2 and *Done when* 1 require both adapters to pass one shared suite.

**Independent Test**: OMDb and TMDb derived fixtures each green for all Wanted cases plus inventory extras (including "transient server error then success") through real typed client, resilience, and quota handler against WireMock; telemetry asserts one span and one duration per call with no key/title/IMDb id tags; ordinary 401 asserts Failed(`InvalidResponse`) + one request + `wasUnauthorized`, with advice text from the fixture hook.

**Acceptance Scenarios**:

1. **Given** the contract suite and an OMDb fixture on shipped DI, **When** each Wanted case and inventory-extra case runs against WireMock, **Then** the case passes (found by IMDb id; found by title and year; not found; ordinary 401 key problem; 429 `RateLimited`; 5xx `ProviderUnavailable`; timeout; unreachable; `Retry-After` inside and past budget; breaker opening; cancellation; transient server error then success; telemetry span/duration and redaction).
2. **Given** the same suite and a TMDb fixture on shipped DI, **When** each Wanted case and inventory-extra case runs, **Then** every case passes with the same shared contract assertions (including ordinary 401 as Failed `InvalidResponse` + one request + `wasUnauthorized`; advice via fixture hook).
3. **Given** ordinary 401 stubs for both adapters, **When** the shared 401 case runs, **Then** the suite asserts the OMDb ordinary-401 key-problem outcome recorded in Scope; any remaining OMDb-vs-TMDb difference is stated in the PR body, or "none" (Q3).
4. **Given** Phase B after DEV-551 implementation is on main, **When** the drift check runs, **Then** harness type names are taken from main and plan/tasks are repinned; no rival harness remains (Q1).

---

### User Story 2 - Twin cases leave the adapter families (Priority: P1)

After the suite is green, every twin across lookup, resilience, and telemetry exists only in the suite. Single-adapter cases and residual adapter-specific assertions stay in their adapter classes. A twin inventory maps each removed test to its contract case before removal.

**Why this priority**: *Wanted* 3 and *Done when* 2; Q4.

**Independent Test**: Twin inventory table (removed-test → contract-case) complete; after removal, no twin remains duplicated in both families; listed single-adapter suites still present; residuals retained where the contract cannot express an adapter-specific assertion.

**Acceptance Scenarios**:

1. **Given** a green contract suite for both adapters, **When** the twin inventory is written, **Then** every twin across lookup, resilience, and telemetry maps to exactly one contract case (including extras beyond *Wanted* 1 such as "transient server error then success"); a twin with no matching case gets a contract case added and is never kept as a residual (`brief.md` Contract case set; Q4).
2. **Given** that inventory, **When** twins are removed family by family with tests green after each family, **Then** those cases exist only in the suite.
3. **Given** TMDb cancel-during-request, OMDb request-limit-body 401 (quota), `MetadataProviderTmdbSignalTests`, `SecondaryMetadataProviderTrailerTests`, `SecondaryMetadataProviderMappingTests`, and composition / chain-composition tests, **When** twin removal finishes, **Then** those single-adapter surfaces remain.
4. **Given** a twin with an adapter-specific assertion the contract cannot express, **When** the twin moves, **Then** only that residual assertion stays in the adapter class (Q4).

---

### User Story 3 - Mutation and line-count receipts with gates (Priority: P2)

Delivery proves Stryker scores on both adapter production files do not drop under identical settings, that the Q5 line-count measure drops, and that all required gates are green on the exact head — including Q7 InspectCode fixes when `SecondaryMetadataProviderTelemetryTests.cs` is touched.

**Why this priority**: *Done when* 3–4; Q5; Q7; closing bar.

**Independent Test**: Paired Stryker before/after receipts per file; line-count before/after under Q5 measure; Roslyn / complexity / InspectCode / `dotnet test` / format green on exact head.

**Acceptance Scenarios**:

1. **Given** Phase B base before edits, **When** Stryker runs on `OmdbMetadataProvider.cs` and `TmdbMetadataProvider.cs`, **Then** before receipts are recorded under identical settings.
2. **Given** the delivery head, **When** Stryker re-runs with the same settings, **Then** neither file’s score drops versus its before receipt.
3. **Given** the Q5 line-count measure whose **before** is the T002 measurement on the Phase B base (5,072 at `e1649d53` is historical context only) and whose after covers the 12 recon files plus new DEV-557 contract/fixture/harness-add lines (excluding DEV-551-merged harness lines), **When** after is compared to before, **Then** after is below before (P10).
4. **Given** `SecondaryMetadataProviderTelemetryTests.cs` appears in the diff, **When** InspectCode runs, **Then** warnings at `:98` and `:102` are fixed; if the file is untouched, those baseline warnings stay `noted, no ticket` (Q7).

---

### Edge Cases

- OMDb request-limit-body 401 (quota) must not be asserted as the shared ordinary-401 key-problem case, and must stay as an OMDb-only case (recon; Q3).
- A twin with no matching contract case gets a case added; it is never retained as a residual (`brief.md` Contract case set).
- Adapter-specific residual assertions that the contract cannot express stay in the adapter class (Q4).
- Fake WireMock keys/tokens only; no real secrets committed (Q6).
- Phase B must not start before DEV-551 implementation merges (Q1).
- Harness type names must not be frozen in this Spec Kit draft before main has them (Q1).
- Chain composition and selection remain out of suite scope (Q2).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Tests MUST provide one abstract metadata-provider contract suite covering every *Wanted* 1 case as the minimum, plus every twin in the inventory (known extra: "transient server error then success"), executed through shipped DI (`MetadataProviderServiceCollectionExtensions.cs:43-59`) with real typed `HttpClient`, real resilience pipeline, and quota handler against WireMock (`brief.md` Contract case set).
- **FR-002**: OMDb and TMDb MUST each run that suite through thin derived fixtures (*Wanted* 2).
- **FR-003**: The suite MUST assert ordinary 401 as Failed `InvalidResponse` after one request with `wasUnauthorized` recording (OMDb key-problem outcome under Current 401 outcomes). Expected key-advice text is supplied by the fixture hook (wording differs per adapter). OMDb request-limit-body 401 quota behaviour MUST NOT be treated as that shared case. TMDb production alignment is allowed only under the Q3 carve-out; the PR body MUST state any remaining difference (or "none").
- **FR-004**: Twin cases across lookup, resilience, and telemetry MUST leave the adapter classes after a removed-test → contract-case inventory; a twin with no matching case MUST gain a contract case and MUST NOT be kept as a residual; single-adapter cases listed in Scope MUST remain; residuals for inexpressible adapter-specific assertions MAY remain (Q4; *Done when* 2; `brief.md` Contract case set).
- **FR-005**: DEV-557 MUST consume the DEV-551 harness and MUST NOT define a rival harness; this spec MUST NOT name harness types; Phase B MUST wait for DEV-551 implementation on main, then drift-check and repin harness names; subtitle-bound harness extraction of provider-agnostic parts is in scope without touching DEV-551 subtitle assertions (Q1).
- **FR-006**: Delivery MUST record paired Stryker before/after receipts on `OmdbMetadataProvider.cs` and `TmdbMetadataProvider.cs` under identical settings with no per-file score drop, and a Q5 line-count drop (FR measure in Scope) (*Done when* 3–4; Q5).
- **FR-007**: Changed files MUST pass Roslyn, cyclomatic complexity (build ≤ 15; refactor gate 6), InspectCode, `dotnet test`, and `dotnet format --verify-no-changes` on the exact head; if `SecondaryMetadataProviderTelemetryTests.cs` is in the diff, InspectCode warnings at `:98` and `:102` MUST be fixed (Q7).
- **FR-008**: WireMock keys and tokens MUST be fake literals only; no real secrets committed (Q6).
- **FR-009**: The suite MUST NOT run against `ChainedMetadataProvider`; chain selection MUST remain unchanged (Q2; *Out of scope*).

### Key Entities

- **Contract suite**: Abstract shared cases over `IMetadataProvider`.
- **Adapter fixture**: Thin OMDb or TMDb derived runner supplying DI registration, options, WireMock stubs, and expected ordinary-401 outcome hooks.
- **Twin inventory**: Mapping from removed adapter-family test to contract case, required before removal and carried in the PR body.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Both OMDb and TMDb pass every Wanted contract case (and inventory extras required by FR-001) through shipped DI against WireMock (*Done when* 1).
- **SC-002**: Every twin that existed in both families across lookup, resilience, and telemetry exists only in the suite, proven by the twin inventory (*Done when* 2).
- **SC-003**: Stryker scores on `OmdbMetadataProvider.cs` and `TmdbMetadataProvider.cs` do not drop versus paired before receipts under identical settings (*Done when* 3; Q5).
- **SC-004**: Q5 line-count measure after is below before (*Done when* 4; Q5).
- **SC-005**: Exact-head gates (Roslyn, complexity, InspectCode, tests, format) are green; Q7 InspectCode rule satisfied; Size M three-axis review (Sentry, Ledger, Compass) completes under the closing bar with a two-round cap.

## Assumptions

- No taste assumptions (`brief.md`; no `ASSUMPTIONS.md`).
- Ticket *Open points* defaults (no chain run; OMDb wins on 401 divergence) are already Patron-ruled in Q2–Q3.
- Phase B file names for contract/fixture classes are pinned only after the DEV-551 harness exists on main (Q1); this Spec Kit draft describes roles, not frozen harness type names.
