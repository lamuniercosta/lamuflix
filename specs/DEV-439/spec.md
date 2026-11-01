# Feature Specification: QuotaExhausted Enrichment Failure Category

**Feature Branch**: `feature/439-spec`
**Created**: 2026-11-01
**Status**: gate1: provisional (plan frozen; 0 new constitution departures; 0 ticket contradictions; ticket-mandated Principle IV constitution-amendment owner checkbox open — Gate 1 stays closed until owner approval/merge)
**Input**: Ticket DEV-439 (parent DEV-100, Size M; prerequisite for DEV-333); frozen `brief.md` (Q1–Q6, P1–P2); `CONCLUSIONS.md`; `ASSUMPTIONS.md` (Q6 `[assumed]` copy); Maestri notes `DEV-439`, `recon-DEV-439`, `recon-DEV-439-2`, `adjudication-DEV-439-plan`; `specs/PRODUCT.md`

## Owner question — Gate 1 stays closed

- [ ] Owner approves the Principle IV closed `EnrichmentFailureCategory` list amendment adding `QuotaExhausted` (constitution Version 1.8.0 → 1.9.0) staged in `.specify/memory/constitution.md`; approval occurs through the Gate 1 spec-PR merge.

Carried from ticket Scope item 6 / AC 6 and Patron Q1 (`brief.md`, `CONCLUSIONS.md`). Patron cannot close this owner gate. Owner rejection → `blocked: structural` for DEV-333 Scope 3. No other structural owner checkbox was identified (Q1; recon-DEV-439:90-103).

## Scope

**In scope** (ticket Scope & Technical Design items 1–6; brief Q1–Q6):

1. **Category**: add `EnrichmentFailureCategory.QuotaExhausted` (const code `quota_exhausted`), `IsRetryable=true`, exact SafeDescription from ASSUMPTIONS Q6, existing recognition conventions, converter `Known` registration; preserve all existing category values and semantics; avoid mutable static initialization in mutated code.
2. **Provider mapping**: `OmdbMetadataProvider` maps `MetadataQuotaExhaustedException` to `Fetch.Rejected(QuotaFailure(EnrichmentFailureCategory.QuotaExhausted))`. Provider HTTP 429 stays `RateLimited` (preserved at `OmdbMetadataProvider.cs:294-317` StatusFailure; primary/fallback proofs at recon-DEV-439-2:28,34-35). Quota persistence failure stays `ProviderUnavailable`. Gate (`IOmdbRequestGate`, `MetadataQuotaHandler`) unchanged. Quota exceptions are caught inside `FetchAsync` before the classifier; `EnrichmentFailureClassifier` stays unchanged and features/embedding allow-lists remain unreachable for quota failures (recon-DEV-439-2:42-48; P2).
3. **Chain**: `ChainedMetadataProvider.IsEligible` includes `QuotaExhausted` wherever `RateLimited` is fallback-eligible today; do not admit other categories.
4. **Settlement parity (including RetryDelayed Action)**: until DEV-333 lands, enrichment settlement handles `QuotaExhausted` exactly as `RateLimited` today — `EnrichmentFailureAction.RetryDelayed`, shared TTL retry-queue transport, terminal Failed after MaxAttempts, and `x-lamuflix-failure-category=quota_exhausted`. P1 mandates the minimal `src/LamuFlix.Core/Features/Enrichment/EnrichmentRetryPolicy.cs:19-21` condition extension so `QuotaExhausted` selects `RetryDelayed` beside `RateLimited` (red parity test first). Other settlement production edits remain AC-forced only. Both `Retry` and `RetryDelayed` already share the TTL retry queue today — there is no immediate-vs-TTL transport split (recon-DEV-439-2:17,53-56). Deferral, deferral column, routing policy and sweeper changes belong to DEV-333.
5. **Glossary**: add the exact `QuotaExhausted` entry to root `CONTEXT.md` (ASSUMPTIONS Q6 copy).
6. **Constitution**: amend Principle IV closed `EnrichmentFailureCategory` list (`.specify/memory/constitution.md:309-314`) to add `QuotaExhausted`, with owner Gate 1 approval above.

**Out of scope** (ticket; brief Q1, Q3, Q5):

- Quota deferral, `EnrichmentDeferredUntil`, delayed re-dispatch queue, claim/sweeper changes (DEV-333).
- Any change to the gate, `omdb_daily_usage`, or `DailyLimit`.
- Repairing the separately observed `FeaturesConsumer` InvalidResponse hardcoding (noted only; no ticket).
- New NuGet/npm dependency, project, top-level folder, architectural layer, migration/DDL, public API/DTO/OpenAPI/web contract, secrets, `Features:LocalPlay`, or `Process.Start`.
- New ADR or new domain model (Q6).

**Precedence**: Ticket Scope & Technical Design is already approved authority (`specs/PRODUCT.md:23-24`). Brief Q1–Q6 and `CONCLUSIONS.md` govern boundary, semantics, edit boundary, tests, ordering, loop discipline, and editorial copy. Phase A produces documents only and claims no implementation gate evidence.

## User Scenarios & Testing

### User Story 1 - Distinct Quota Admission Failure (Priority: P1)

As the enrichment pipeline (and eventual DEV-333 deferral), I need a local OMDb daily-quota admission denial classified as `QuotaExhausted` so it can be told apart from a provider HTTP 429 (`RateLimited`) without changing retry policy yet.

**Why this priority**: ticket deliverable; prerequisite contract for DEV-333 (Q1–Q2).

**Independent Test**: category/unit tests for code, SafeDescription, recognition, `IsRetryable=true`, and unchanged existing values; converter + real Testcontainers DB round-trip for `quota_exhausted`.

**Acceptance Scenarios**:

1. **Given** the closed category set **When** `QuotaExhausted` is read **Then** its code is `quota_exhausted`, SafeDescription is exactly `Local OMDb daily request quota admission was denied.`, and `IsRetryable` is true (Q2, Q6).
2. **Given** existing categories **When** recognition/description/retryability are exercised **Then** their values and semantics are unchanged (Q2, Q3).
3. **Given** code `quota_exhausted` **When** it round-trips through the converter and a real database **Then** the singleton category is restored (ticket AC; Q4).

### User Story 2 - Provider Mapping Preserves Neighbors (Priority: P1)

As the OMDb adapter, I need gate admission denial mapped to `QuotaExhausted` with zero transport calls, while provider HTTP 429 remains `RateLimited` and quota persistence failure remains `ProviderUnavailable`.

**Why this priority**: ticket Scope item 2; AC 1–2 (Q2–Q4).

**Independent Test**: in `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderUnitTests.cs`, compose real `MetadataQuotaHandler` (denied StubGate via scoped factory, RecordingHandler inner) through the provider to `QuotaExhausted` with zero inner transport calls; keep isolated ThrowingHandler/fallback/429/persistence checks; independently prove HTTP 429 → `RateLimited` (recon-DEV-439-2:28,34-35) and persistence failure → `ProviderUnavailable` (recon-DEV-439-2:65-67).

**Acceptance Scenarios**:

1. **Given** the daily quota gate denies admission **When** OMDb fetch runs **Then** the result is `QuotaExhausted` and zero transport calls are made (ticket AC; Q4).
2. **Given** the provider returns HTTP 429 **When** OMDb fetch runs **Then** the result is `RateLimited` (ticket AC; Q2).
3. **Given** quota-gate persistence failure **When** OMDb fetch runs **Then** the result remains `ProviderUnavailable` (Q2, Q3).
4. **Given** the gate and handler **Then** their behavior and wiring are unchanged (ticket Scope item 2).

### User Story 3 - Chain Fallback Parity with RateLimited (Priority: P1)

As the chained metadata provider, I need `QuotaExhausted` from OMDb to be fallback-eligible exactly where `RateLimited` is, so a TMDb success still completes the item, without expanding eligibility to unrelated categories.

**Why this priority**: ticket Scope item 3; AC 3 (Q2–Q3).

**Independent Test**: OMDb returns `QuotaExhausted`, TMDb succeeds → chain result is the TMDb success; noneligible categories stay noneligible.

**Acceptance Scenarios**:

1. **Given** OMDb returns `QuotaExhausted` and TMDb succeeds **When** the chain runs **Then** the chain result is the TMDb success (ticket AC; Q3).
2. **Given** a noneligible failure category from the primary **When** the chain runs **Then** fallback does not proceed for that category (Q3).
3. **Given** `IsEligible` **Then** it includes `QuotaExhausted` wherever `RateLimited` is eligible and does not newly admit other categories (Q3).

### User Story 4 - Settlement Parity Until DEV-333 (Priority: P1)

As enrichment settlement, I need `QuotaExhausted` to follow today’s `RateLimited` settlement — including `EnrichmentFailureAction.RetryDelayed`, shared TTL retry-queue transport, failure-header emission (`quota_exhausted`), and terminal Failed-at-MaxAttempts — without introducing deferral.

**Why this priority**: ticket Scope item 4; AC 4 (Q3–Q4; P1).

**Independent Test**: compare `QuotaExhausted` and `RateLimited` for `RetryDelayed` Action, retry attempts, TTL delay, `x-lamuflix-failure-category` values, and terminal Failed at MaxAttempts across Decide, both settlement handlers, routing, and consumer header paths (recon-DEV-439-2:9-23,60-63).

**Acceptance Scenarios**:

1. **Given** a `QuotaExhausted` failure below MaxAttempts **When** settlement runs **Then** Decide returns `RetryDelayed` exactly as for `RateLimited`, and both actions share the existing TTL retry queue (ticket AC; Q4; P1; recon-DEV-439-2:9,17,53-56).
2. **Given** a `QuotaExhausted` failure at MaxAttempts **When** settlement runs **Then** the movie becomes terminal Failed as with `RateLimited` (Q4).
3. **Given** a dead-letter/failure publish **When** headers are written **Then** `x-lamuflix-failure-category` is `quota_exhausted` (policy compatibility, not identical category text) (Q4).
4. **Given** this ticket **Then** no deferral column, next-day re-dispatch, routing-policy change, or sweeper change is introduced (Q1, Q3; P1).

### User Story 5 - Glossary, Constitution, and Phase Boundaries (Priority: P2)

As owners and implementers, I need the exact glossary entry and Principle IV list amendment drafted in Phase A, with owner Gate 1 approval before Phase B coding, and Size M review under the Q5 closing bar after implementation.

**Why this priority**: ticket Scope items 5–6; AC 6; Q4–Q6.

**Independent Test**: `CONTEXT.md` contains the exact assumed glossary sentence; constitution staged 1.9.0 lists `QuotaExhausted`; owner checkbox remains open until Gate 1 merge.

**Acceptance Scenarios**:

1. **Given** Phase A drafting **Then** `CONTEXT.md` carries the exact Q6 glossary wording and the constitution Principle IV closed list includes `QuotaExhausted` under Version 1.9.0 staged for owner review (Q6).
2. **Given** Gate 1 **Then** the constitution amendment remains an explicit unresolved owner approval; Patron cannot close it (Q1, Q6).
3. **Given** Phase B after owner approval **Then** build/analyzer/test/mutation gates run via harness scripts (mutation without `--since` from worktrees); exit 2 SKIPPED/SKIP/N/A and Could not run are never PASS; Size M requires Sentry, Ledger, and Compass under the Q5 bar (Q4, Q5).

### Edge Cases

- Local quota admission denial must not be labelled `RateLimited` after this change (Q2).
- Provider HTTP 429 must not be labelled `QuotaExhausted` (Q2).
- Quota persistence failure remains `ProviderUnavailable`, not `QuotaExhausted` (Q2).
- Chain eligibility expands only by adding `QuotaExhausted` beside existing `RateLimited` eligibility; unrelated categories stay out (Q3).
- Settlement parity includes `RetryDelayed` Action plus attempts/TTL delay/terminal Failed; header text is the category code, not identical prose (Q4; P1). `Retry` and `RetryDelayed` already share the TTL retry queue — do not claim an immediate-vs-TTL transport split (recon-DEV-439-2:17,53-56).
- Quota exceptions never reach `EnrichmentFailureClassifier` on the enrichment path; classifier and features/embedding allow-lists stay unchanged (recon-DEV-439-2:42-48; P2).
- Old readers whose `Known` set lacks `quota_exhausted` reject that persisted code — rollback/mixed-version reading of newly written values carries a compatibility risk; no tolerant-reader, migration, or rolling-deploy support is in scope (recon-DEV-439-2:10; R6).
- `FeaturesConsumer` InvalidResponse hardcoding remains untouched; Q3 authorized no follow-up ticket (Q3; S-L2).
- Existing branch diff in `FeaturesConsumerTests.cs` is preserved and not attributed to this task (brief).
- Anything outside frozen scope is a follow-up issue, not a finding in this round (Q5).

## Requirements

### Functional Requirements

- **FR-001**: Add `EnrichmentFailureCategory.QuotaExhausted` in `src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs` with const code `quota_exhausted`, `IsRetryable=true`, SafeDescription exactly `Local OMDb daily request quota admission was denied.`, and existing recognition conventions; preserve all existing category values/semantics; use const literals and avoid mutable static initialization in mutated code (Q2, Q6; recon-DEV-439:13-14).
- **FR-002**: Register `QuotaExhausted` in `EnrichmentFailureCategoryConverter.Known` so `quota_exhausted` round-trips through persistence (ticket AC; Q3–Q4; recon-DEV-439:14).
- **FR-003**: Change only the `MetadataQuotaExhaustedException` mapping in `OmdbMetadataProvider` to `Fetch.Rejected(QuotaFailure(EnrichmentFailureCategory.QuotaExhausted))`. Keep provider HTTP 429 → `RateLimited` (recon-DEV-439-2:28,34-35) and `MetadataQuotaPersistenceException` → `ProviderUnavailable`. Do not change `IOmdbRequestGate` or `MetadataQuotaHandler`. Do not add quota knowledge to `EnrichmentFailureClassifier` or edit features/embedding allow-lists (recon-DEV-439-2:42-48; P2; Q2–Q3; recon-DEV-439:18-23).
- **FR-004**: Extend `ChainedMetadataProvider.IsEligible` in `src/LamuFlix.Infrastructure/Adapters/ChainedMetadataProvider.cs` so `QuotaExhausted` is fallback-eligible wherever `RateLimited` is; do not newly admit other categories (Q3; recon-DEV-439:66).
- **FR-005**: Prove enrichment settlement emits `quota_exhausted` on `x-lamuflix-failure-category` with the same `EnrichmentFailureAction.RetryDelayed`, attempt/TTL-delay/terminal-Failed-at-MaxAttempts behavior as `RateLimited` across `EnrichmentRetryPolicy.Decide`, `ProcessEnrichmentCommandHandler`, `RecordEnrichmentFailureCommandHandler`, routing, and consumer headers (recon-DEV-439-2:9-23,60-63). P1 mandates the minimal condition extension at `src/LamuFlix.Core/Features/Enrichment/EnrichmentRetryPolicy.cs:19-21` so `QuotaExhausted` selects `RetryDelayed` beside `RateLimited` (red parity test first; mandatory green reproof). Other settlement production edits remain AC-forced only. Introduce no deferral, routing-policy, or sweeper change; both actions already share the TTL retry queue (recon-DEV-439-2:17,53-56; P1; Q3–Q4).
- **FR-006**: Tests covering (Q4; P1; recon-DEV-439-2:60-67): (1) category code/description/recognition/retryability and unchanged existing values — `tests/LamuFlix.UnitTests/Domain/EnrichmentFailureCategoryTests.cs`, `tests/LamuFlix.UnitTests/Persistence/ValueConverterTests.cs`; (2) composed real `MetadataQuotaHandler` (denied `StubGate` via scoped factory, `RecordingHandler` inner transport) through `OmdbMetadataProvider` in `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderUnitTests.cs` → `QuotaExhausted` with zero inner transport calls; keep isolated ThrowingHandler mapping, fallback, HTTP 429 → `RateLimited` (`tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderUnitTests.cs`, `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderFallbackUnitTests.cs`, recon-DEV-439-2:28,34-35), and persistence-failure → `ProviderUnavailable` checks — handler-only tests never substitute for the composed seam; (3) OMDb `QuotaExhausted` then TMDb success in `tests/LamuFlix.UnitTests/Adapters/MetadataProviderChainTests.cs`; noneligible categories stay noneligible; (4) mandatory RateLimited/QuotaExhausted settlement parity (RetryDelayed Action, attempts, TTL delay, headers, terminal Failed) in `tests/LamuFlix.UnitTests/Features/Enrichment/EnrichmentRetryPolicyTests.cs`, `tests/LamuFlix.UnitTests/Features/Enrichment/ProcessEnrichmentCommandHandlerTests.cs`, `tests/LamuFlix.UnitTests/Features/Enrichment/RecordEnrichmentFailureCommandHandlerTests.cs`, `tests/LamuFlix.UnitTests/RabbitMq/EnrichmentRoutingTests.cs`, and `tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs` (TTL/header behavior); (5) converter + real Testcontainers DB round-trip with no mocked driver. Regression inventory (run, not denial-proof edits): `tests/LamuFlix.UnitTests/Features/Enrichment/EnrichmentFailureClassifierSurvivorTests.cs` (existing survivor suite; no new quota classifier case — recon-DEV-439-2:42-48,62), `tests/LamuFlix.UnitTests/Adapters/MetadataQuotaHandlerTests.cs`, `tests/LamuFlix.IntegrationTests/EfMetadataRequestGateTests.cs`, `tests/LamuFlix.IntegrationTests/EfMetadataRequestGateConcurrencyTests.cs` (gate suites regression-run only; do not alter the gate). No EF/Testcontainers required for the composed unit zero-transport seam.
- **FR-007**: Add the exact glossary entry to root `CONTEXT.md`: `QuotaExhausted is an enrichment failure caused by denial of admission under the local OMDb daily request quota; it is distinct from provider HTTP 429 and does not itself imply deferral.` (Q6; Scope item 5).
- **FR-008**: Stage constitution MINOR amendment 1.8.0 → 1.9.0 adding `QuotaExhausted` to the Principle IV closed `EnrichmentFailureCategory` list (and aligning the cited retry parenthetical so `QuotaExhausted` shares the TTL retry-queue path with `RateLimited` until DEV-333). Owner Gate 1 checkbox above must remain unchecked until owner approval. No new ADR (Q1, Q6; Governance written proposal + Sync Impact Report).
- **FR-009**: Edit only ticket/brief-named surfaces; preserve the existing unrelated `FeaturesConsumerTests.cs` branch diff; do not repair `FeaturesConsumer` InvalidResponse hardcoding; no public API/web/secrets/LocalPlay/Process.Start; no new dependency/project/layer/migration; no DEV-333 deferral work (Q3, Q5).
- **FR-010**: Phase B verification after Gate 1: Roslyn analyzers, cyclomatic complexity (then refactor threshold), InspectCode, unit/integration/property/full suite, vulnerable packages, format, pre-PR mutation via `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` from a worktree. Exit 0 = Pass; exit 1 = Failure; exit 2 = SKIPPED (scope-empty) / SKIP (opt-out) / mutation N/A — never PASS; Could not run is never Pass. Closing bar: zero retained Critical/High/Medium; every Low explicitly dispositioned; all three axes Sentry/Ledger/Compass required; round cap two; missing axis or blocking gate stays blocked. Do not lower thresholds. No merge authorised by this spec (Q4–Q5).

### Key Entities

- **QuotaExhausted**: enrichment failure category for local OMDb daily request quota admission denial; code `quota_exhausted`; retryable; distinct from provider HTTP 429; does not itself imply deferral.
- **RateLimited**: existing category for provider throttling (including HTTP 429); unchanged.
- **ProviderUnavailable**: existing category retained for quota-gate persistence failure; unchanged.
- **EnrichmentFailureCategoryConverter.Known**: persisted-value recognition set that must include the new category; unknown codes are rejected (recon-DEV-439-2:10).
- **EnrichmentRetryPolicy.Decide**: selects `RetryDelayed` for `RateLimited` today at lines 19–21; P1 extends that condition to cover `QuotaExhausted`.

## Success Criteria

- **SC-001**: A gate rejection produces `QuotaExhausted` and makes zero transport calls (ticket AC).
- **SC-002**: A provider HTTP 429 still produces `RateLimited` (ticket AC; recon-DEV-439-2:28,34-35).
- **SC-003**: With OMDb returning `QuotaExhausted` and TMDb succeeding, the chain result is the TMDb success (ticket AC).
- **SC-004**: Settlement of a `QuotaExhausted` failure matches today’s `RateLimited` settlement for `RetryDelayed` Action, attempts, TTL delay, headers, and terminal Failed (ticket AC; P1; recon-DEV-439-2:9,17,53-56).
- **SC-005**: `quota_exhausted` round-trips through persistence (ticket AC).
- **SC-006**: Constitution amendment is owner-approved at Gate 1 before merge (ticket AC); Phase A keeps the checkbox open; Patron cannot close it; T021 records verified owner answer + merge receipt only and does not infer approval (Q1; R7).
- **SC-007**: Applicable analyzer/complexity/InspectCode/format/test/mutation gates pass or report SKIPPED/SKIP/N/A/Could not run distinctly — never as PASS (Q4–Q5).

## Assumptions

- Exact SafeDescription and CONTEXT.md glossary copy are Patron Q6 taste rulings logged `[assumed]` in `ASSUMPTIONS.md`.
- Recon baseline HEAD in the brief is planning authority only; Phase B pickup must refresh recon for main/contract/identity-map drift (brief; R2).
- No new ADR is required (Q6).
- P1 supersedes Q3 conditional-edit wording solely for `EnrichmentRetryPolicy.Decide` (mandatory minimal QuotaExhausted/`RetryDelayed` extension with red-first proof). Other settlement production edits remain AC-forced only (P1; Q3).
- Existing TTL retry-queue mechanism already serves both `Retry` and `RetryDelayed`; constitution TTL wording is retained; no routing edit (recon-DEV-439-2:53-56; R3).

## Clarifications

All grill categories were resolved by Patron Q1–Q6 before drafting; plan-challenge P1–P2 dispositions and R1–R8 repairs are recorded in `CONCLUSIONS.md` / `adjudication-DEV-439-plan`. `/speckit-clarify` scan: Functional Scope, Domain/Data, Interaction, NFR, Integration/Dependencies, and Out-of-scope — **Clear**. No `[NEEDS CLARIFICATION]` markers remain. One owner Gate 1 checkbox remains open (constitution 1.9.0 Principle IV list); there is no present owner approval.

## Traceability

| Source | Encoded as |
|---|---|
| Ticket Scope 1–6 / AC 1–6 | Scope; US1–US5; FR-001–FR-010; SC-001–SC-006 |
| Q1 delivery boundary + owner gate | Owner question; Scope; FR-008; SC-006 |
| Q2 category semantics / vocabulary | US1–US3; FR-001, FR-003–FR-004 |
| Q3 minimal edit boundary (P1 supersedes Decide site only) | Scope; FR-003–FR-005, FR-009; Edge Cases |
| Q4 tests / ordering / gates | US1–US4; FR-006, FR-010; plan Ordering |
| Q5 loop discipline | FR-010; plan Gates |
| Q6 editorial copy + grill closure | ASSUMPTIONS; FR-001, FR-007–FR-008; Clarifications |
| P1 RetryDelayed Decide edit + Action parity | Scope 4; US4; FR-005; SC-004; plan Approach/Structure; T009–T011 |
| P2 dispositions / R1–R8 | Edge Cases; FR-003, FR-005–FR-006; Assumptions; plan Risks/Test Strategy |
| recon-DEV-439 / recon-DEV-439-2 paths | FR-001–FR-006; plan Structure |
