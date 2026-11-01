# Implementation Plan: QuotaExhausted Enrichment Failure Category

**Branch**: `feature/439-spec` | **Date**: 2026-11-01 | **Spec**: `specs/DEV-439/spec.md`
**Input**: Frozen `brief.md` (Q1–Q6, P1–P2); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-439`, `recon-DEV-439`, `recon-DEV-439-2`, `adjudication-DEV-439-plan`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (staged 1.9.0 Principle IV list for Gate 1)
**Status**: Draft — Phase 2 plan-challenge repair round 1 (R1–R8); Gate 1 not approved (owner checkbox open); PLAN FROZEN not declared; no implementation clearance.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`, `constitution-amendment-v1.9.0.md` (proposal record), root `CONTEXT.md` glossary entry, staged `.specify/memory/constitution.md` 1.9.0. No `research.md`, `data-model.md`, or `contracts/` — every technical choice is ruled in the brief, and the public contract chain is untouched. No new ADR (Q6).

## Summary

Give OMDb daily-quota admission denial its own enrichment failure category `QuotaExhausted` (`quota_exhausted`, retryable) instead of reusing `RateLimited`, so DEV-333 can later defer quota-rejected items without conflating them with provider HTTP 429. Map only `MetadataQuotaExhaustedException` in `OmdbMetadataProvider`; extend chain fallback eligibility beside `RateLimited`; prove settlement parity with today’s `RateLimited` path including `EnrichmentFailureAction.RetryDelayed` via the P1-mandated minimal `EnrichmentRetryPolicy.Decide` condition extension; persist via existing converter; document in `CONTEXT.md` and amend Principle IV’s closed list with owner Gate 1 approval. No gate/schema/deferral/routing-policy/public-API work.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`TreatWarningsAsErrors=true`, nullable enabled, CPM).
**Primary Dependencies**: Existing pins only. **No new packages** (brief §2.3; recon-DEV-439:40).
**Storage**: Existing enrichment failure-category string persistence via `EnrichmentFailureCategoryConverter`; no new table or migration.
**Testing**: xUnit v3, Shouldly; UnitTests for category/converter/provider/chain/settlement; composed real `MetadataQuotaHandler` + denied StubGate + RecordingHandler zero-transport seam in `OmdbMetadataProviderUnitTests.cs` (no EF); IntegrationTests with Testcontainers strictly for real DB round-trip. Gate suites (`EfMetadataRequestGateTests` / concurrency) regression-run only. No live OMDb. No mocked EF/driver serialization behaviour.
**Target Platform**: .NET server (ASP.NET Core Minimal API host hosting enrichment consumers).
**Project Type**: Domain enumeration extension + Infrastructure mapping/eligibility + docs/constitution.
**Performance Goals**: unchanged; gate rejection still makes zero transport calls.
**Constraints**: const code literal; no mutable static initialization in mutated code; `TimeProvider` where time is involved; no explanatory comments outside allowed exemptions; complexity ≤ 15 then refactor gate; never hardcode/lower thresholds; no public API/secret/LocalPlay; P1 mandates minimal `EnrichmentRetryPolicy.Decide` QuotaExhausted/`RetryDelayed` extension; other settlement production edits only if AC-forced.
**Scale/Scope**: personal library; Size M; contract prerequisite for DEV-333.

## Constitution Check

Gates checked against `.specify/memory/constitution.md` (staged Version 1.9.0 on this branch for Gate 1; prior baseline 1.8.0). Ticket Scope item 6 + Patron Q1/Q6 require amending the Principle IV closed `EnrichmentFailureCategory` list. No new ADR (Q6). Owner Gate 1 checkbox in `spec.md` stays unchecked until merge approval. Quill stages the amendment and proposal record; Patron cannot close the owner gate.

| Principle | Gate | Plan response |
|---|---|---|
| IV (Enrichment state machine) | Closed `EnrichmentFailureCategory` list; category-driven retry; caller-safe messages; single classifier | **CONDITIONAL PASS (owner Gate 1)**. Add `QuotaExhausted` to the closed list; SafeDescription + `IsRetryable=true`; settlement retains RateLimited-equivalent `RetryDelayed` + shared TTL retry until DEV-333 (P1); classifier unchanged — quota exceptions stay caught in the provider before Classify (recon-DEV-439-2:42-48; P2). No new constitutional single-classifier rewrite or departure. Noncompliant implementation unauthorized until owner answers the checkbox. |
| I (Ports and Adapters) | No new ports | PASS — no new port; gate/handler unchanged. |
| II / V (Failures / API boundary) | Domain failures stay caller-safe; no raw exception text | PASS — new category carries SafeDescription; no public API shape change. |
| VI (Observability) | Existing failure headers/logs | PASS — consumer continues to emit category code on `x-lamuflix-failure-category` (`quota_exhausted`). |
| VII (Configuration / time) | No new options; TimeProvider | PASS — no options/schema change; existing TimeProvider usage retained where settlement timing appears. |
| VIII (Ubiquitous Language) | Glossary in CONTEXT.md | PASS — exact Q6 glossary entry added; no vendor-name Core type invented. |
| IX (Test Pyramid) | Real DB for persistence; no mocked driver translation | PASS — converter + Testcontainers round-trip; real handler zero-transport proof; unit chain/settlement parity. |
| Governance | Written proposal; version bump; Sync Impact; template review; ADR only when architectural choice changes | PASS path — proposal record `constitution-amendment-v1.9.0.md`; staged 1.9.0; templates reviewed (no edit); **no ADR** per Patron Q6 (enumeration extension, not a new architectural choice). Owner approval still required at Gate 1. |
| Gates | Roslyn, complexity, InspectCode, format, vulnerable packages, mutation | Phase B only; see tasks verification phase. |

### Complexity Tracking

No unjustified constitution violation. The Principle IV list change is the ticket-mandated amendment path with explicit owner Gate 1 approval — not a silent departure and not a waiver.

## Approach

- **Phase A (this draft)**: spec/plan/tasks + exact `CONTEXT.md` glossary + constitution 1.9.0 staging + proposal record; owner checkbox open; plan-challenge R1–R8 applied; Keel read-back before any CLEAN/PLAN FROZEN claim.
- **Category/persistence first (Q4)**: add `QuotaExhausted` with const code, SafeDescription, `IsRetryable`, recognition; register converter `Known`; unit + Testcontainers round-trip.
- **Provider mapping**: change only `MetadataQuotaExhaustedException` → `QuotaExhausted`; prove composed real MetadataQuotaHandler → provider zero transport; lock HTTP 429 → `RateLimited` (recon-DEV-439-2:28,34-35) and persistence → `ProviderUnavailable`.
- **Chain**: add `QuotaExhausted` to `IsEligible` beside `RateLimited`; prove TMDb success fallback; keep noneligible categories out.
- **Settlement parity (P1)**: red-first proof that `QuotaExhausted` must select `RetryDelayed` like `RateLimited`; mandatory minimal edit at `src/LamuFlix.Core/Features/Enrichment/EnrichmentRetryPolicy.cs:19-21`; mandatory green reproof across Decide, both settlement handlers, routing/delay, headers, and MaxAttempts terminal Failed. Both actions already share the TTL retry queue (recon-DEV-439-2:17,53-56) — no immediate-vs-TTL claim. Unconditional settlement inspection; other settlement production edits remain AC-forced only. No deferral/routing-policy change.
- **Verification**: full harness gates + Size M three-axis review under Q5 bar after Gate 1; classifier survivor suite and gate suites in regression inventory only.

## Project Structure

```text
specs/DEV-439/
  brief.md, CONCLUSIONS.md, ASSUMPTIONS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  constitution-amendment-v1.9.0.md
  checklists/requirements.md

CONTEXT.md                                                    (edit: QuotaExhausted glossary)
.specify/memory/constitution.md                               (stage 1.9.0 Principle IV list)

src/LamuFlix.Core/
  Domain/EnrichmentFailureCategory.cs                         (edit: QuotaExhausted)
  Features/Enrichment/EnrichmentRetryPolicy.cs                (edit: Decide :19-21 — P1 QuotaExhausted beside RateLimited → RetryDelayed)

src/LamuFlix.Infrastructure/
  Persistence/Converters/EnrichmentFailureCategoryConverter.cs (edit: Known)
  Adapters/OmdbMetadataProvider.cs                            (edit: exhaustion mapping only)
  Adapters/ChainedMetadataProvider.cs                         (edit: IsEligible)

# Settlement production — unconditional inspection (recon-DEV-439-2:9-23); further edits AC-forced only:
#   src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentCommandHandler.cs
#   src/LamuFlix.Core/Features/Enrichment/RecordEnrichmentFailureCommandHandler.cs
#   src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs
#   src/LamuFlix.Infrastructure/RabbitMq/EnrichmentRouting.cs  (inspect only — Retry/RetryDelayed already share TTL queue)

tests/LamuFlix.UnitTests/   (mandatory suites named in FR-006 / T006 / T009 — edit what AC/P1 require)
tests/LamuFlix.IntegrationTests/  (Testcontainers DB round-trip; EnrichmentConsumerTests TTL/header; gate suites regression-run only)
```

**Layout decisions**:

- No research/data-model/contracts artifacts — public contract chain untouched; decisions frozen in brief.
- Preserve existing unrelated `tests/LamuFlix.IntegrationTests/FeaturesConsumerTests.cs` branch diff; do not attribute to this task.
- Quill owns spec/plan/tasks/receipt/checklist/amendment proposal/`CONTEXT.md`/staged constitution list edit. Quill does not edit `brief.md` / `CONCLUSIONS.md` / ADR bodies (none). Keel owns analyze/plan challenge.

## Test Strategy

| Surface | Coverage |
|---|---|
| Category (US1) | code `quota_exhausted`; exact SafeDescription; `IsRetryable=true`; recognition; existing values unchanged — `tests/LamuFlix.UnitTests/Domain/EnrichmentFailureCategoryTests.cs` |
| Converter + DB (US1) | `Known` registration; `tests/LamuFlix.UnitTests/Persistence/ValueConverterTests.cs`; real Testcontainers persistence round-trip; no mocked driver. Old readers without `quota_exhausted` in Known reject it (compatibility risk noted; no tolerant reader) |
| Provider (US2) | `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderUnitTests.cs`: real MetadataQuotaHandler resolves denied StubGate via scoped factory, wraps RecordingHandler, used by provider HttpClient → QuotaExhausted + zero inner transport calls (recon-DEV-439-2:65-67). Keep isolated ThrowingHandler mapping, fallback, HTTP 429 → RateLimited (recon-DEV-439-2:28,34-35), persistence → ProviderUnavailable. Handler-only tests never substitute. No EF/Testcontainers for this unit seam |
| Chain (US3) | `tests/LamuFlix.UnitTests/Adapters/MetadataProviderChainTests.cs`: OMDb QuotaExhausted + TMDb success; noneligible stay noneligible; IsEligible includes QuotaExhausted with RateLimited |
| Settlement (US4) | Mandatory: `EnrichmentRetryPolicyTests` Decide RetryDelayed Action parity (red before Decide edit); `ProcessEnrichmentCommandHandlerTests`; `RecordEnrichmentFailureCommandHandlerTests`; `EnrichmentRoutingTests`; `tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs` TTL/header behavior (recon-DEV-439-2:60-63). Attempts/TTL delay/`x-lamuflix-failure-category=quota_exhausted`/terminal Failed. No deferral/routing-policy change |
| Regressions | `tests/LamuFlix.UnitTests/Features/Enrichment/EnrichmentFailureClassifierSurvivorTests.cs` (existing; no new quota classifier case); `tests/LamuFlix.UnitTests/Adapters/MetadataQuotaHandlerTests.cs`; `tests/LamuFlix.IntegrationTests/EfMetadataRequestGateTests.cs` / `EfMetadataRequestGateConcurrencyTests.cs` regression-run only; FeaturesConsumer hardcoding untouched |

No Gherkin. Property tests only if harness requires for changed surface — report SKIP/SKIPPED/N/A distinctly.

## Gates (Phase B — after Gate 1 and implementation)

1. Explicit changed-C# scope: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness, `./scripts/run-jetbrains-inspectcode.ps1`.
2. Affected unit/integration suites, then full `dotnet test`.
3. Property / vulnerable-packages / format per harness; distinguish PASS, failure, SKIPPED, SKIP, mutation N/A, Could not run.
4. Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` (no `--since` from worktree).
5. Size M review: Sentry, Ledger, Compass; Q5 closing bar; two remediation rounds max.

Phase A runs none of these and claims no gate result.

## Risks

- **Mis-mapping HTTP 429**: accidental QuotaExhausted labelling — mitigated by dedicated RateLimited preservation tests (Q4; recon-DEV-439-2:28,34-35).
- **Settlement Action drift**: without the P1 Decide edit, QuotaExhausted falls to plain `Retry` instead of `RetryDelayed` — mitigated by red T009 Action-parity test → mandatory T010 Decide edit → mandatory T011 green reproof (P1; recon-DEV-439-2:9,71). Transport is already shared TTL for both actions (recon-DEV-439-2:17,53-56); do not claim an immediate-vs-TTL split.
- **Over-editing settlement**: unnecessary policy/routing/deferral changes — mitigated by unconditional inspection plus AC-forced rule for sites other than the known Decide edit (Q3; P1).
- **Persisted-code compatibility**: old readers lacking `quota_exhausted` in Known reject newly persisted values — rollback/mixed-version risk noted; no tolerant-reader, migration, or rolling-deploy support in scope (recon-DEV-439-2:10; R6).
- **Constitution without owner approval**: implementation before Gate 1 — blocked by unchecked checkbox and task Phase 0; T021 records verified owner answer + merge receipt only and does not infer approval (Q1; R7).
- **Pickup drift**: brief recon baseline ages — mandatory T000 Phase B pickup (fresh recon including identity-map/contract drift + cross-artifact reanalysis; authorized drift task updates only; stop on unresolved drift) before T001 coding (brief; R2).
- **FeaturesConsumer divergence**: out of scope; noted only; Q3 authorized no follow-up ticket (Q3; S-L2).

## Ordering

See `tasks.md`. Sequence and dependency diagram:

```text
Phase 0  T021 (Gate 1 record) — blocks T000 and T001–T020
Phase B  T000 (pickup/drift + identity-map/contract recheck) — blocks T001–T020
Phase 1  T001 → T002 → T003 → T004   (T003 after both T001 and T002)
Phase 2  T005 → T006
Phase 3  T007 → T008
Phase 4  T009 → T010 → T011   (all mandatory: red parity → Decide edit → green reproof)
Phase 5  T012 → T020
```

Gate 1 record (after owner approval; receipt only) → Phase B pickup (fresh recon including identity-map/contract drift + cross-artifact reanalysis against landed main/contracts; authorized task updates only for demonstrated drift; **STOP** on unresolved drift) → category/persistence → provider mapping (T006 composed real MetadataQuotaHandler + denied StubGate + RecordingHandler zero-transport in OmdbMetadataProviderUnitTests) → chain → settlement (mandatory T009 red RetryDelayed parity → T010 minimal Decide edit + unconditional inspection → T011 green reproof) → verification/review. Glossary and constitution staging are Phase A (already drafted here); owner approval must precede Phase B pickup and coding.
