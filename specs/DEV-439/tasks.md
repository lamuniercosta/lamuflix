# Tasks: QuotaExhausted Enrichment Failure Category

**Input**: `specs/DEV-439/` spec.md, plan.md, frozen brief.md (Q1–Q6, P1–P2), CONCLUSIONS.md, ASSUMPTIONS.md, recon-DEV-439, recon-DEV-439-2, note DEV-439, adjudication-DEV-439-plan, specs/PRODUCT.md
**Status**: PLAN FROZEN; owner Gate 1 approved through spec PR #301; T021 receipt recorded and T000 Phase B pickup cleared at `993eeebb3068608e232503aaef84422af8885b32`. See the current T021/T000 receipts in `CONCLUSIONS.md`; T001–T020 remain outstanding.
**Gate 1**: Owner checkbox in `spec.md` must be resolved before any production implementation task (T001–T020) executes. Constitution 1.9.0 Principle IV list is staged into `.specify/memory/constitution.md` and proposal record `constitution-amendment-v1.9.0.md` is prepared in this Phase A / spec PR. After verified owner approval: Keel records the verified checkbox answer + verified spec-PR merge receipt in `CONCLUSIONS.md` (T021) — receipt only; does not create a ruling or infer approval. Patron cannot close the owner gate. Owner rejection → `blocked: structural` (DEV-333 Scope 3). Owner approval and merge are verified in the T021 receipt; earlier open-gate statements in Phase A artifacts are historical. No new ADR (Q6).
**Phase B pickup**: T000 complete and cleared at the pinned HEAD (receipt in `CONCLUSIONS.md`); the required pickup procedure was: fresh recon including identity-map/contract drift + cross-artifact reanalysis against landed main/contracts; authorized task updates only for demonstrated drift; stop on unresolved drift.
**Tests**: Required (ticket ACs; Q4). TDD optional; tests land with each step; required coverage green before verification.
**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`.
**Conventions**: `Method_Condition_Expected`; `TestContext.Current.CancellationToken`; only `// arrange` / `// act` / `// assert`; no new explanatory comments; const code literal; no mutable static initialization in mutated code; no live OMDb; no mocked EF/driver serialization; preserve unrelated `FeaturesConsumerTests.cs` branch diff.

---

## Phase dependency / execution order

```text
Phase 0  T021 (Gate 1 record) — blocks T000 and T001–T020
Phase B  T000 (pickup/drift + identity-map/contract recheck) — blocks T001–T020
Phase 1  T001 → T002 → T003 → T004   (T003 after both T001 and T002)
Phase 2  T005 → T006
Phase 3  T007 → T008
Phase 4  T009 → T010 → T011   (all mandatory: red parity → Decide edit → green reproof)
Phase 5  T012 → T020
```

Phase A already drafted glossary (`CONTEXT.md`) and constitution staging; those are not re-done in Phase B except verify-as-needed after merge / authorized drift repair at T000.

---

## Phase 0: Gate 1 — constitution 1.9.0 (blocks Phase B pickup and all implementation)

**Purpose**: Record owner Gate 1 answer for the Principle IV closed-list amendment before category code lands.
**Checkpoint**: Owner approved; Keel recorded checkbox + merge receipt in `CONCLUSIONS.md`; or stop blocked structural.

- [x] T021 **Keel** records the verified owner Gate 1 checkbox answer for the constitution 1.9.0 Principle IV `QuotaExhausted` amendment from `spec.md` **plus the verified spec-PR merge receipt** in `specs/DEV-439/CONCLUSIONS.md`. Receipt only — does not create a ruling or infer approval. Keep the sole owner constitution-amendment checkbox unchecked and blocking until actual owner approval/merge; Patron cannot close it. Stop if unchecked or rejected (`blocked: structural`). T000 and T001–T020 MUST NOT proceed until this recorded answer and merge receipt exist (Q1; FR-008; SC-006; R7).

---

## Phase B: Pickup — fresh recon and drift reanalysis (blocks T001–T020)

**Purpose**: Confirm the frozen brief/spec/plan/tasks still match landed main and contracts before any production edit; refresh recon; re-analyze cross-artifact.
**Checkpoint**: Fresh recon recorded; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift; unresolved drift stops for a cited decision — no invention.

- [x] T000 After T021: obtain fresh Phase B pickup recon against picked HEAD / landed main and contracts, including fresh identity-map/contract drift recheck for settlement/retry/header paths (do not treat the Phase A recon baseline as current-state clearance; recon-DEV-439-2:9-23). Run cross-artifact reanalysis of brief/spec/plan/tasks against landed main/contracts (category/converter/provider/handler/chain/settlement/glossary/constitution surfaces; recheck HTTP 429, classifier boundary, TTL mechanism citations — recon-DEV-439-2:28,34-35,42-48,53-56). Amend tasks only where authorized by demonstrated drift through the assigned workflow. **STOP** on unresolved drift or missing contract facts for a cited decision (`needs recon` / cited ruling) — do not invent substitutes. No production edits in this task (brief Phase B ordering 2; FR-009–FR-010; R2–R3).

---

## Phase 1: User Story 1 — Category and persistence (P1)

**Goal**: `QuotaExhausted` exists with correct semantics and persists as `quota_exhausted`.
**Independent Test**: unit category/converter tests + Testcontainers DB round-trip.

- [x] T001 [US1] Add `EnrichmentFailureCategory.QuotaExhausted` in `src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs` with const code `quota_exhausted`, SafeDescription exactly `Local OMDb daily request quota admission was denied.`, `IsRetryable=true`, existing recognition conventions; preserve all existing members/semantics; avoid mutable static initialization (FR-001; Q2, Q6)
- [x] T002 [US1] Register `EnrichmentFailureCategory.QuotaExhausted` in `Known` inside `src/LamuFlix.Infrastructure/Persistence/Converters/EnrichmentFailureCategoryConverter.cs` (FR-002; Q3)
- [x] T003 [US1] After both T001 and T002: extend unit tests in `tests/LamuFlix.UnitTests/Domain/EnrichmentFailureCategoryTests.cs` and `tests/LamuFlix.UnitTests/Persistence/ValueConverterTests.cs` for code/SafeDescription/retryability/recognition, unchanged existing values, and converter round-trip of `quota_exhausted` (FR-006; Q4; R8)
- [x] T004 [US1] Add/extend real-database Testcontainers persistence round-trip proving `quota_exhausted` restores the singleton category (no mocked driver) using the existing integration persistence patterns (FR-006; Q4; SC-005)

---

## Phase 2: User Story 2 — Provider mapping (P1)

**Goal**: Gate rejection → `QuotaExhausted` with zero transport; neighbors preserved.
**Independent Test**: provider/handler tests for exhaustion, HTTP 429, persistence failure.

- [x] T005 [US2] Edit `src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.cs` so `MetadataQuotaExhaustedException` maps to `Fetch.Rejected(QuotaFailure(EnrichmentFailureCategory.QuotaExhausted))`; leave HTTP 429 → `RateLimited` and `MetadataQuotaPersistenceException` → `ProviderUnavailable`; do not change gate/handler (FR-003; Q2–Q3)
- [x] T006 [US2] In `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderUnitTests.cs`, add the composed zero-transport proof: real `MetadataQuotaHandler` resolves a denied `StubGate` through a scoped factory, wraps a `RecordingHandler`, and is used as the provider `HttpClient` handler; assert returned `QuotaExhausted` and zero inner transport calls (recon-DEV-439-2:65-67). Keep isolated ThrowingHandler exhaustion mapping, fallback coverage in `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderFallbackUnitTests.cs`, HTTP 429 → `RateLimited` (recon-DEV-439-2:28,34-35), and persistence-failure → `ProviderUnavailable` checks. Handler-only tests (`tests/LamuFlix.UnitTests/Adapters/MetadataQuotaHandlerTests.cs`) are extras, never substitutes. No EF/Testcontainers for this unit seam. `tests/LamuFlix.IntegrationTests/EfMetadataRequestGateTests.cs` and `tests/LamuFlix.IntegrationTests/EfMetadataRequestGateConcurrencyTests.cs` are regression-run only — do not edit the gate for denial proof (FR-006; Q4; SC-001–SC-002; R4–R5)

---

## Phase 3: User Story 3 — Chain fallback (P1)

**Goal**: `QuotaExhausted` is fallback-eligible with `RateLimited`; TMDb success completes.
**Independent Test**: `MetadataProviderChainTests` (and composition coverage if required).

- [x] T007 [US3] Edit `src/LamuFlix.Infrastructure/Adapters/ChainedMetadataProvider.cs` `IsEligible` to include `QuotaExhausted` wherever `RateLimited` is eligible; do not newly admit other categories (FR-004; Q3)
- [x] T008 [US3] Extend `tests/LamuFlix.UnitTests/Adapters/MetadataProviderChainTests.cs` (and composition tests only if AC requires) proving OMDb `QuotaExhausted` + TMDb success completes, and noneligible categories stay noneligible (FR-006; Q4; SC-003)

---

## Phase 4: User Story 4 — Settlement parity (P1)

**Goal**: `QuotaExhausted` matches today’s `RateLimited` settlement including `RetryDelayed` Action, shared TTL transport, header `quota_exhausted`, and terminal Failed; no deferral.
**Independent Test**: mandatory Decide/handler/routing/consumer tests comparing both categories (recon-DEV-439-2:60-63).

- [ ] T009 [US4] Write red-first settlement parity covering `EnrichmentFailureAction.RetryDelayed` Action, retry attempts, TTL delay, `x-lamuflix-failure-category` (`quota_exhausted`), and terminal Failed at MaxAttempts versus `RateLimited`. Mandatory suites (no discretionary selection): `tests/LamuFlix.UnitTests/Features/Enrichment/EnrichmentRetryPolicyTests.cs` (must first fail on missing RetryDelayed Action parity for QuotaExhausted), `tests/LamuFlix.UnitTests/Features/Enrichment/ProcessEnrichmentCommandHandlerTests.cs`, `tests/LamuFlix.UnitTests/Features/Enrichment/RecordEnrichmentFailureCommandHandlerTests.cs`, `tests/LamuFlix.UnitTests/RabbitMq/EnrichmentRoutingTests.cs`, and `tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs` for TTL/header behavior (FR-005–FR-006; Q4; SC-004; P1; R1–R2; recon-DEV-439-2:60-63)
- [ ] T010 [US4] Unconditionally inspect settlement production paths `src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentCommandHandler.cs`, `src/LamuFlix.Core/Features/Enrichment/RecordEnrichmentFailureCommandHandler.cs`, `src/LamuFlix.Core/Features/Enrichment/EnrichmentRetryPolicy.cs`, `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs`, and `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentRouting.cs`. Perform the P1-mandated minimal Decide condition extension at `EnrichmentRetryPolicy.cs:19-21` so `QuotaExhausted` selects `RetryDelayed` beside `RateLimited`. Other settlement production edits remain AC-forced only. Introduce no deferral/sweeper/routing policy; both Retry and RetryDelayed already share the TTL queue (FR-005; Q3; P1; R1–R2; recon-DEV-439-2:9,17,53-56)
- [ ] T011 [US4] Mandatory: re-run the full T009 parity suite green after T010; keep FeaturesConsumer InvalidResponse hardcoding untouched (FR-005, FR-009; Q3; P1; R1; R8)

---

## Phase 5: Polish & Phase B verification

- [ ] T012 Confirm frozen scope only: no public API/DTO/OpenAPI/TypeScript, no LocalPlay/Process.Start/secrets, no new NuGet/project/layer/migration, no DEV-333 deferral, no FeaturesConsumer hardcoding repair, unrelated `FeaturesConsumerTests.cs` diff preserved, glossary/constitution entries present as drafted (FR-007–FR-009; Q5)
- [ ] T013 Run Roslyn analyzers on explicit changed `.cs` set via `./scripts/run-roslyn-analyzers.ps1` (FR-010; Q4)
- [ ] T014 Run `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness.yml on changed files (FR-010)
- [ ] T015 Run `./scripts/run-jetbrains-inspectcode.ps1` on changed files (FR-010)
- [ ] T016 `dotnet format --verify-no-changes` clean (FR-010)
- [ ] T017 Run affected unit/integration suites then full `dotnet test`, including regression inventory `tests/LamuFlix.UnitTests/Features/Enrichment/EnrichmentFailureClassifierSurvivorTests.cs` (existing survivor suite; no new quota classifier case), `tests/LamuFlix.UnitTests/Adapters/MetadataQuotaHandlerTests.cs`, and gate suites `tests/LamuFlix.IntegrationTests/EfMetadataRequestGateTests.cs` / `tests/LamuFlix.IntegrationTests/EfMetadataRequestGateConcurrencyTests.cs` as regression-run only (FR-006, FR-010; SC-007; R3–R5)
- [ ] T018 Run property/security/vulnerable-package gates per harness; report SKIP/SKIPPED/N/A/Could not run distinctly — never as PASS (FR-010; Q5)
- [ ] T019 Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` from the worktree (FR-010; Q4)
- [ ] T020 Size M code review with Sentry, Ledger and Compass; apply Q5 closing bar (Critical/High/Medium block; Lows explicit disposition) and 2-round remediation cap; missing axis is blocked; do not lower thresholds (FR-010; Q5)

---

## Dependencies

- Phase A glossary + constitution staging: already done in this drafting ask — not gated on T021; Phase B does not re-author them unless authorized drift repair at T000.
- T021 before T000; T000 before any T001–T020 (stable IDs; explicit Gate 1 → pickup → implementation/verification order).
- Phase 1 before Phase 2. T001 before T002; T003 after both T001 and T002; T004 after T002.
- Phase 2 after Phase 1. T005 before T006.
- Phase 3 after Phase 2 mapping exists (category must exist for eligibility). T007 before T008.
- Phase 4 after Phase 2 (category emitted by provider). T009 → T010 → T011 all mandatory (red RetryDelayed parity → P1 Decide edit + unconditional inspection → green reproof).
- T012–T020 after implementation and mandatory AC evidence (T003–T004, T006, T008, T009–T011) complete.

## Sequencing examples

```text
# Phase 0
T021 Keel records verified Gate 1 checkbox answer + verified spec-PR merge receipt in CONCLUSIONS.md (receipt only; no inferred approval)

# Phase B pickup
T000 fresh recon including identity-map/contract drift + cross-artifact reanalysis vs landed main/contracts; authorized drift task updates only; STOP on unresolved drift

# Phase 1
T001 EnrichmentFailureCategory.QuotaExhausted
T002 EnrichmentFailureCategoryConverter.Known
T003 category + converter unit tests (after both T001 and T002)
T004 Testcontainers persistence round-trip

# Phase 2
T005 OmdbMetadataProvider exhaustion → QuotaExhausted
T006 OmdbMetadataProviderUnitTests composed MetadataQuotaHandler+denied StubGate+RecordingHandler zero-transport; keep isolated/fallback/429/persistence checks; gate suites regression-only

# Phase 3
T007 ChainedMetadataProvider.IsEligible += QuotaExhausted
T008 chain fallback tests

# Phase 4
T009 red settlement parity (RetryDelayed Action + attempts/TTL/headers/terminal) across mandatory suites
T010 unconditional inspection + mandatory EnrichmentRetryPolicy.Decide :19-21 QuotaExhausted→RetryDelayed edit; other settlement edits AC-forced only
T011 mandatory green reproof of T009 suite

# Phase 5
T012–T020 scope confirm + gates (incl. classifier survivor + gate-suite regression) + Size M review
```

## MVP

MVP delivers the ticket contract with all code ACs: T021 (Gate 1 receipt), T000 (Phase B pickup), T001–T011 (settlement inspection, P1 Decide edit, and T011 green reproof are mandatory — not optional), covering category/persistence, provider mapping, chain fallback, and settlement Action/TTL/header/terminal parity. Gates T012–T020 follow.

## Notes

- `[P]` unused in this ticket’s linear dependency chain except where explicitly marked; prefer sequential safety on same conceptual surface.
- Do not edit files outside the brief/recon frozen set without a cited decision amendment (Q5). Missing facts → `needs recon`, not invention.
- T000 is the mandatory Phase B pickup for fresh recon / cross-artifact reanalysis / authorized drift repair; stop on unresolved drift (brief).
- Constitution amendment is owner-gated; no ADR (Q6).
