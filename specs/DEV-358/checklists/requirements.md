# Specification Quality Checklist: TMDb Keywords, Full Cast, and Collection Signals in MovieFeatures

**Purpose**: Validate specification completeness and quality before planning/analyze
**Created**: 2026-11-01
**Feature**: [spec.md](../spec.md)
**Status**: Draft — OWNER DECISION PENDING (Q6). Plan Challenge Round 1 repairs R1–R10 applied. Gate 1 closed. Not PLAN FROZEN. Repair verification / PLAN FROZEN / Gate 1 / build pickup / gate-metric PASS not claimed.

## Content Quality

- [x] No implementation details leaking as the only requirement form — paths appear where the brief Q11 manifest and ticket name them (as amended by R3/R5/R6/R7)
- [x] Focused on enrichment, recommendation, and measurement outcomes (signals in MovieFeatures, franchise top-10, HR@10 receipt)
- [x] Stakeholder-readable scenarios with cited Q-rulings for implementers
- [x] All mandatory sections completed (Owner checkbox, Scope, Stories, Requirements, Success, Assumptions, Clarifications, Traceability)

## Requirement Completeness

- [x] No `[NEEDS CLARIFICATION]` markers invented by Quill; sole unresolved user choice is the verbatim unticked Q6 owner checkbox
- [x] Requirements are testable and unambiguous against recon-DEV-358-2:7-19, Q7–Q11, and Plan Challenge R1–R10
- [x] Success criteria are measurable (persistence/features, HitCount bar, contiguous top-10, disabled parity, gates)
- [x] Success criteria avoid prescribing unstated frameworks beyond ticket/brief stack
- [x] Acceptance scenarios defined per user story (ingest/supplement, persist/features, seed grouping, paired evaluation)
- [x] Edge cases identified (unavailable vs known-empty, partial cast, disabled parity, DEV-424 clear supersession, null collection term, evaluation empty coverage, embedding-less union, Q6 amendment rule)
- [x] Scope clearly bounded; no new dependency/port/layer/public API/frontend/LocalPlay/secret; no generic MMR redesign; no fabricated replay; no automatic backfill; README/CHANGELOG excluded
- [x] Dependencies and assumptions identified; Q1 Phase B recon; ASSUMPTIONS.md [assumed] contracts + Round 1 amendments; Q6 Gate 1 closed

## Feature Readiness

- [x] Functional requirements map to acceptance criteria / Q-rulings / R1–R10
- [x] User scenarios cover primary flows (ingest, persist/score, seed grouping, evaluation)
- [x] Feature meets measurable outcomes in Success Criteria
- [x] No implementation readiness, plan freeze, or Gate 1 clearance claimed by this draft
- [x] Bounded Q11 file set and eight-step task order encoded in plan/tasks (with Round 1 manifest deltas)
- [x] `/speckit-clarify` coverage Clear across scope/domain/NFR/integration — residual is owner checkbox, not Quill clarification
- [x] `/speckit-checklist` requirements quality items above all pass

## Domain checklist (DEV-358-specific)

- [x] Verbatim unticked Q6 collection FK vs join-table owner checkbox present in spec.md (brief Owner decision; ASSUMPTIONS.md; CONCLUSIONS adjudication)
- [x] Q7 assumed keyword/credits shapes, identity/dedup/full-cast/fail-closed rules encoded (FR-001–FR-002; US1)
- [x] Q8 supplemental merge after OMDb Found, base-metadata preservation, Unavailable vs KnownEmpty encoded (FR-003–FR-004; US1; R1/R4)
- [x] Care-item-3 `keywords`/`movie_keywords`/`movie_tmdb_cast` atomic persistence encoded; collection FK conditional on Q6 (FR-005–FR-006; US2; R10 provenance name retained)
- [x] Per-signal save table + DEV-424 clear-on-OMDb supersession for unavailable/disabled collection encoded (FR-005–FR-006; R1)
- [x] Tracked-load / deterministic locks / atomic rollback encoded (FR-005; plan data model/Risks; T013/T015/T016; R2)
- [x] MovieFeatures Keywords (`ImmutableHashSet<int>`)/CollectionId, enabled effective Cast as base ∪ `FullCast`, SimilarityWeights defaults 1.0/2.0 encoded (FR-007–FR-008; R3)
- [x] Functionality-based Core names `MovieKeyword` / `MovieSignalSnapshot` / `FullCast`; nested availability enum; no `MovieCastEntry`; snapshot collection availability only; vendor `Tmdb*` restricted to Infrastructure/`TmdbOptions` (R3 amends C1; Constitution VIII)
- [x] Six `CONTEXT.md` glossary entries + Package 2 glossary task encoded (C2/R3; Constitution VIII :408)
- [x] One logical TMDb invocation + typed supplemental outcome matrix + corrected Constitution IV row encoded (FR-001/FR-003; plan Constitution IV; R4)
- [x] WireMock cases in IntegrationTests only; UnitTests signal HTTP file removed; Shouldly/NSubstitute/AutoFixture/Theory/MemberData named; large-cast smoke without SLA; zero raw api_key URI logging (R5)
- [x] SeedCollectionGrouper contiguous top-10 + indexed candidate union (embedding-less eligible) + pure grouper/no fourth port + no MmrSelector/API change encoded (FR-009; US3; R9)
- [x] Incremental enrichment / no automatic backfill documented; leading-block taste cost retained (FR-015; R9)
- [x] Paired `-TmdbSignals` evaluation via `EvaluationEntry` → `EvaluationReplayOptions.Default`, explicit fixture weights, independent keyword+collection coverage, tracked `specs/DEV-358/comparison.md`, ignored raw JSON, separate seed proof encoded (FR-010–FR-011; US4; R6–R8)
- [x] Amended Q11 exact edit set (add EvaluationEntry.cs; remove UnitTests signal HTTP; tracked comparison.md), strict eight-package order, Package 8 BLOCKED/FAIL lock, gate list encoded (FR-012–FR-014; plan/tasks; I2; R5–R7)
- [x] SC-005 requires automated coverage + gate evidence without mandatory red-first receipt (I3)
- [x] Q12 closing bar, two-round plan-challenge caps, three-axis requirement, frozen exclusions encoded (plan Loop discipline; spec Out of scope; R10)
- [x] README/CHANGELOG exclusion and narrative-currency residual noted, no ticket; no constitution departure/new owner escalation (R10)
- [x] No new SDK/project/port/layer/public API/frontend/LocalPlay/secret/ADR claimed
- [x] Q11 secondary research/data-model/quickstart/contracts filenames folded into plan.md under formal Patron I1 authorization (receipt)
- [x] Historical Q1–Q12 exchanges preserved verbatim in brief/CONCLUSIONS (not rewritten by Quill)

## Plan Challenge Round 1 repair coverage (R1–R10)

- [x] R1 availability table + DEV-424 clear supersession in spec/plan/tasks
- [x] R2 tracked-load deterministic locks and atomicity in plan/tasks
- [x] R3 FullCast immutable representation, named availability, one collection identity
- [x] R4 supplemental sequence/outcome matrix and correct constitution IV
- [x] R5 WireMock integration placement and independent/large-cast cases
- [x] R6 tracked `specs/DEV-358/comparison.md` receipt
- [x] R7 `EvaluationEntry.cs` bounded correction and pure `EvaluationReplayOptions.Default` transport
- [x] R8 explicit positive fixture weights and independent exercised term coverage
- [x] R9 incremental enrichment and embedding-less eligible union
- [x] R10 disposition/checklist/receipt propagation (this checklist + DRAFTING_RECEIPT)

## Notes

- Quill invented no decisions; behaviour taken only from brief/CONCLUSIONS/ASSUMPTIONS/ticket/recon/adjudication.
- Q6 remains user-owned; Patron recommendation is not approval; Gate 1 stays closed; checkbox remains verbatim unticked.
- External TMDb schemas remain not available in tree; assumed contracts tagged in ASSUMPTIONS.md.
- `/speckit-analyze` is Keel-owned; Quill did not run it.
- Plan Challenge Round 1 repairs applied; five High findings pending Keel read-back. Plan freeze / Gate 1 / build pickup / gate-metric PASS remain unclaimed.
