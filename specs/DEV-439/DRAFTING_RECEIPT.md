# DEV-439 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-11-01
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-439-spec` · branch `feature/439-spec`
**Method**: `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` in one ask (plus requirements checklist, CONTEXT glossary, constitution 1.9.0 staging, amendment proposal, this receipt). Grill already closed (`brief.md` Q6); mandatory `before_specify` grill hook not re-executed. `/speckit-analyze` not run by Quill (Keel owns read-only analyze / plan challenge). `.specify/feature.json` already pointed at `specs/DEV-439` and was left unchanged.

**Inputs**: frozen `specs/DEV-439/brief.md` (Q1–Q6, P1–P2), `CONCLUSIONS.md`, `ASSUMPTIONS.md` (Q6 `[assumed]` copy), Maestri notes `DEV-439`, `recon-DEV-439` (esp. ticket-text follow-up lines 50–103), `recon-DEV-439-2`, `adjudication-DEV-439-plan`, `specs/PRODUCT.md`, `.specify/memory/constitution.md` (Principle IV list amendment authorized by ticket Scope item 6).

**Artifacts written / updated (Quill-owned)**:

- `specs/DEV-439/spec.md`
- `specs/DEV-439/plan.md`
- `specs/DEV-439/tasks.md`
- `specs/DEV-439/checklists/requirements.md`
- `specs/DEV-439/constitution-amendment-v1.9.0.md` (proposal record)
- `CONTEXT.md` (QuotaExhausted glossary entry — Scope item 5; exact ASSUMPTIONS Q6 copy)
- `.specify/memory/constitution.md` (staged Version 1.9.0 Principle IV closed-list + retry parenthetical alignment — Scope item 6)
- this receipt

**Not edited by Quill**: `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, ADR bodies (none for this ticket), production code.

**Owner checkbox**: present and unchecked in `spec.md` — Owner approves constitution 1.9.0 Principle IV `QuotaExhausted` list amendment; approval occurs through the Gate 1 spec-PR merge. Not a waiver; no present owner approval. Owner rejection → `blocked: structural` for DEV-333 Scope 3. PLAN FROZEN not declared. T021 is receipt-only (verified owner answer + merge receipt); Patron cannot close the gate (R7).

**Decisions invented by Quill**: none. Behaviour taken only from brief/CONCLUSIONS/ASSUMPTIONS/ticket/recon facts and Patron-approved P1/P2 / R1–R8.

**Encoding notes (not new decisions)**:

- Exact SafeDescription and CONTEXT glossary sentences copied from `ASSUMPTIONS.md` Q6 `[assumed]` rulings.
- Constitution retry parenthetical updated within the ticket-cited `:309-314` block so `QuotaExhausted` shares the TTL retry-queue path with `RateLimited`, matching brief Q2/Q4 settlement-parity rulings (delayed retry until DEV-333). List membership otherwise only adds `QuotaExhausted`.
- Pre-existing constitution closed-list omission of `DuplicateImdbId` (present in code) was left untouched — out of ticket Scope item 6; not invented as an amendment target.
- `ChainedMetadataProvider` path resolved from recon + worktree as `src/LamuFlix.Infrastructure/Adapters/ChainedMetadataProvider.cs` (IsEligible presently includes RateLimited and ProviderUnavailable; tasks add QuotaExhausted wherever RateLimited is eligible without newly admitting other categories).
- P1 supersedes Q3 conditional settlement-edit wording solely for `EnrichmentRetryPolicy.Decide`; other settlement production edits remain AC-forced (P1).
- No `research.md` / `data-model.md` / `contracts/` — public contract chain untouched; decisions frozen in brief (precedent DEV-426).
- Unrelated branch diff `tests/LamuFlix.IntegrationTests/FeaturesConsumerTests.cs` called out to preserve; FeaturesConsumer InvalidResponse hardcoding excluded (Q3; S-L2 — no follow-up ticket).

## Phase 2 Spec Kit analyze repair round 1 (Keel I1, U1)

Applied Keel 2-item numbered correction list only; no other edits.

1. **I1 MEDIUM**: Added blocking Phase B pickup task `T000` before `T001` in `tasks.md` (fresh recon; cross-artifact reanalysis against landed main/contracts; authorized task updates for demonstrated drift; **STOP** on unresolved drift). Mirrored dependency in `plan.md` Ordering and task diagram (`T021` → `T000` → `T001`…).
2. **U1 MEDIUM**: Amended `T006` in `tasks.md` to require composed real `MetadataQuotaHandler` through `OmdbMetadataProvider` execution path, counting downstream transport calls as zero, while retaining independent HTTP 429 → `RateLimited` and persistence-failure → `ProviderUnavailable` checks. Mirrored Provider (US2) row in `plan.md` Test Strategy.

## Phase 2 plan-challenge repair round 1 (Patron P2 / adjudication R1–R8)

Applied all 8 repairs from note `adjudication-DEV-439-plan` to `spec.md`, `plan.md`, and `tasks.md`. No other edits. No production code. No PLAN FROZEN / CLEAN claim.

1. **R1 — Explicit settlement action parity**: Synchronized Scope 4 / US4 / FR-005 / SC-004 / Assumptions, plan Approach/Structure/Risks/Ordering, and T009–T011 with P1. Planned minimal `src/LamuFlix.Core/Features/Enrichment/EnrichmentRetryPolicy.cs:19-21` QuotaExhausted beside RateLimited → RetryDelayed. T009 red-first on missing RetryDelayed Action parity; T010 performs the minimal edit; T011 mandatory green reproof. Removed any immediate-vs-TTL claim (both actions share TTL queue — recon-DEV-439-2:17,53-56). Other settlement production edits remain AC-forced.
2. **R2 — Mandatory settlement proof and pickup inventory**: Removed discretionary and/or suite selection. Named mandatory suites from recon-2:60-63 including `tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs`. T010 inspection unconditional. T000 obtains fresh identity-map/contract drift recon. Supporting tests authorized; no new routing/deferral policy.
3. **R3 — Evidence anchors and classifier boundary**: Cited recon-2:28,34-35 (HTTP 429), :42-48 (caught quota exceptions; unchanged classifier; unreachable features/embedding allow-lists), :53-56 (existing TTL mechanism). Included `tests/LamuFlix.UnitTests/Features/Enrichment/EnrichmentFailureClassifierSurvivorTests.cs` in FR-006/T017 regression inventory. Pickup recheck mandated. No new quota classifier knowledge, features allow-list edit, or constitutional single-classifier rewrite.
4. **R4 — Mandatory zero-transport composition**: T006 names `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderUnitTests.cs` composed real MetadataQuotaHandler + denied StubGate via scoped factory + RecordingHandler → QuotaExhausted + zero inner transport. Kept isolated ThrowingHandler/fallback/429/persistence checks; handler-only tests never substitute. No EF/Testcontainers for this unit seam; DB round-trip remains real Testcontainers.
5. **R5 — Precise paths and regression boundary**: Fully qualified evidenced production and test paths; removed if-present / relative-project / and/or ambiguity. Marked `EfMetadataRequestGateTests.cs` / `EfMetadataRequestGateConcurrencyTests.cs` regression-run only. Did not invent missing paths or alter the gate.
6. **R6 — Persisted-code compatibility risk**: Documented that old readers lacking `quota_exhausted` in Known reject it (rollback/mixed-version risk; recon-2:10). No tolerant-reader, migration, or rolling-deploy support claimed or added.
7. **R7 — Owner receipt only**: T021 scoped to verified owner checkbox answer + verified spec-PR merge receipt; does not create a ruling or infer approval. Sole owner constitution-amendment checkbox remains unchecked and blocking; Patron cannot close it.
8. **R8 — Dependencies and MVP**: T003 follows both T001 and T002. Settlement inspection, P1 Decide edit, and T011 reproof are mandatory in dependencies, diagrams, and MVP text. Stable task IDs retained; explicit T021 → T000 → implementation/verification order.

**needs decision:** none.

**needs recon:** none for this repair round. Phase B pickup (`T000`) still requires fresh recon for main/contract/identity-map drift per brief before coding.

**Clarify**: coverage Clear; no `[NEEDS CLARIFICATION]` markers; Gate 1 checkbox open.

**Status**: Phase 2 plan-challenge repair round 1 (R1–R8) applied to `spec.md`, `plan.md`, `tasks.md`, and this receipt. Gate 1 not approved. Quill does **not** claim PLAN FROZEN, CLEAN, owner approval, or implementation clearance. No production gates run. Next: Keel read-back of repairs as Conductor routes.
