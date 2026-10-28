# Specification Quality Checklist: Persist TMDb Collections and Collection Facet Filter

**Purpose**: Validate specification completeness and quality before planning/analyze
**Created**: 2026-10-28
**Feature**: [spec.md](../spec.md)
**Status**: PLAN FROZEN — Keel Phase 2 step 6 adjudication; see PLAN_CHALLENGE.md. Gate 1 remains closed.

## Content Quality

- [x] No implementation details leaking as the only requirement form — paths appear where the frozen brief and ticket name them (records, endpoints, Filter Panel, generated contracts)
- [x] Focused on library-user and enrichment outcomes (membership, facet discovery, filter, URL chips)
- [x] Stakeholder-readable scenarios with cited Q-rulings for implementers
- [x] All mandatory sections completed (Scope, Stories, Requirements, Success, Assumptions, Clarifications, Traceability)

## Requirement Completeness

- [x] No `[NEEDS CLARIFICATION]` markers remain; Sentry F7 closed by Patron ruling @ `10ffc9c1` (deliberate `belongs_to_collection` whole-response Failed InvalidResponse rule)
- [x] Requirements are testable and unambiguous against ticket lines 27–30, Q1–Q6, step 6 rulings, and F7
- [x] Success criteria are measurable (persistence proofs, facet payloads, browse validation, UI interactions, contract drift, gates)
- [x] Success criteria avoid prescribing unstated frameworks beyond ticket/brief stack
- [x] Acceptance scenarios defined per user story (enrichment membership, facet, browse filter, Filter Panel)
- [x] Edge cases identified (success clear vs failure preserve, fallback clear residual, F7 malformed-present Failed-not-Found + missing/null valid-absent, orphans F5 out of scope, unknown id toolbar-only, helper naming collision, forced concurrency retry, no backfill)
- [x] Scope clearly bounded; backfill, grid grouping, provenance/merging/cleanup, new dependency/layer, orphan telemetry excluded
- [x] Dependencies and assumptions identified; DEV-331/DEV-322 Phase B recheck noted; taste in ASSUMPTIONS.md; design.md exists with disposition pending

## Feature Readiness

- [x] Functional requirements map to acceptance criteria / Q-rulings / step 6 rulings / F7
- [x] User scenarios cover primary flows (persist, facet, browse filter, chips/URL)
- [x] Feature meets measurable outcomes in Success Criteria
- [x] No implementation readiness, challenge freeze, or Gate 1 clearance claimed by this draft
- [x] Bounded file set and genre-mirror approach encoded
- [x] `/speckit-clarify` coverage Clear across scope/domain/NFR/integration categories including closed F7
- [x] `/speckit-checklist` requirements quality items above all pass

## Domain checklist (collections-specific)

- [x] Optional collection id/name on TMDb mapping + MovieMetadata matches Q1
- [x] CollectionRecord TMDb int PK ValueGeneratedNever + nullable MovieRecord.CollectionId + generated migration with CollectionId index matches ticket/Q2/step 6 ruling 1/Sentry F6
- [x] Required whole-save rollback/reset/re-read/retry across reachable save paths (recon-DEV-424-2: existing retry does not classify collection PK conflicts; ambient metadata writes have no retry loop; preserve caller transaction ownership; no partial retry in aborted ambient transaction; no invented extra infrastructure), forced concurrency oracle, fallback-clear + failure-preserves, F7 malformed-present Failed-not-Found + missing/null valid-absent match Q2/Q5/step 6 rulings 1–2 / F7
- [x] GET /api/collections GetCollections CollectionDto/CollectionFacet (int Id, string Name, int MovieCount); no empty validator; unchanged AddHandler (recon-DEV-424-2 lines 39–45 empty-validator pass-through confirmed); omits zero members; no filter-narrowed counts (Q3; step 6 ruling 5)
- [x] Browse string? CollectionId / CollectionIdKey / ReadNumber / repeated-key / MovieQueryValidator / codec rejection / empty-abc-0--3-repeated → 422 match C1 + step 6 ruling 4
- [x] Filter Panel fieldset/legend + sr-only checkbox toggle, toolbar-only fallback retained loading/error/empty, legend-only empty, no skeleton/aria-busy match Q4 + step 6 ruling 3 + ASSUMPTIONS.md
- [x] Hostile plain-text + overlong-layout web oracles (Sentry F4)
- [x] Backend tests alongside; green before T020; survivor test files named; web red-first (step 6 ruling 6; Ledger F3); T003/T004 require malformed-present Failed-not-Found and missing/null valid-absent oracles
- [x] Contract chain C# → OpenAPI → generated TS only (brief scope 8)
- [x] No rename of HasSameCollectionValues / SameCollections (Q8)
- [x] No new NuGet/npm, project, layer, LocalPlay/secrets/Process.Start, or ADR claimed
- [x] Loop discipline (closing bar, frozen scope, two-round cap, size M three-axis) matches Q7
- [x] Task order matches Q6 five-step sequence as amended by step 6

## Notes

- Clarify pass recorded in `spec.md` Clarifications — Sentry F7 residual closed by Patron ruling @ `10ffc9c1`; no open drafting `needs decision`.
- Analyze C1 resolved within constitution (`CONCLUSIONS.md` C1 @ `2754a161`): invalid `collectionId` → 422; no owner checkbox. Historical Q3 400 wording preserved; Compass disposition: bottom-section supersede annotation sufficient (no further edit).
- Plan-challenge correction round 1 applied from Keel numbered list + Patron step 6 rulings @ `7a335ac3`.
- Plan-challenge correction round 2 applied: F7 deliberate `belongs_to_collection` rule + recon-DEV-424-2 retry/AddHandler evidence.
- Sentry F5 orphan telemetry/cleanup: out of scope, noted no fix.
- Quill does not author ADRs (Q8). Quill did not edit `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `CONTEXT.md`, or `design.md`.
- `/speckit-analyze` is Keel-owned; Quill did not run it.
- Challenge disposition/freeze and Gate 1 remain unclaimed.

