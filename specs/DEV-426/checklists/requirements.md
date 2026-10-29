# Specification Quality Checklist: OMDb Daily Request Quota

**Purpose**: Validate specification completeness and quality before planning/analyze
**Created**: 2026-10-28
**Feature**: [spec.md](../spec.md)
**Status**: Draft remediated for plan-challenge ROUND 2 FINAL (narrow documentation residuals; Round 1 closures retained) — ready for Keel final verify; Gate 1 not approved; owner checkbox open; PLAN FROZEN not declared; amendment + ADR-0031 Proposed prepared in this spec PR

## Content Quality

- [x] No implementation details leaking as the only requirement form — technical paths appear where the frozen brief, ticket, and PC1/PC2 name them (port, options, table, functional support types, handler seam, FetchAsync mapping)
- [x] Focused on free-tier budget protection and enrichment-operator outcomes
- [x] Stakeholder-readable scenarios with cited Q-rulings and PC1/PC2 for implementers
- [x] All mandatory sections completed (Owner question, Scope, Stories, Requirements, Success, Assumptions, Clarifications, Traceability)

## Requirement Completeness

- [x] No `[NEEDS CLARIFICATION]` markers remain — grill Q1–Q6 closed; post-grill C1/C2/I1 and PC1/PC2 applied; gaps would escalate as `needs decision:` / `needs recon:` in DRAFTING_RECEIPT only
- [x] Requirements are testable and unambiguous against ticket deliverables and Q1–Q6 + C1/C2/I1 + PC1/PC2
- [x] Success criteria are measurable (concurrency N-of->N, restart exhaustion, UTC rollover, exact attempt counts, both-signal mapping/health/partials, startup validation, gates)
- [x] Success criteria avoid prescribing unstated frameworks beyond ticket/brief stack
- [x] Acceptance scenarios defined per user story (shared budget, atomic admission, physical-attempt seam, options/schema, verification)
- [x] Edge cases identified (midnight sampling, cross-midnight retry, first-row race, mid-retry exhaustion, partial discard, DB failure, historical count above lowered limit, lost reservations, denied-attempt DB cost, mismatched-host precondition, frozen-scope follow-ups)
- [x] Scope clearly bounded; per-key budgets, cleanup, hot reload, cache, startup-log, public API, LocalPlay, DEV-333, wider VIII exception out; ADR-0031 + constitution 1.7.0 amendment path in scope for C1/C2
- [x] Dependencies and assumptions identified; recon baseline refresh required at Phase B pickup
- [x] Unchecked owner Gate 1 checkbox present for ADR-0031 + constitution 1.7.0; no present owner approval claimed; rejection path is `blocked: structural`

## Feature Readiness

- [x] Functional requirements map to ticket + Q1–Q6 + C1/C2/I1 + PC1/PC2
- [x] User scenarios cover primary flows including both local signals, health unchanged, and partial discard
- [x] Feature meets measurable outcomes in Success Criteria
- [x] Phase A claims no implementation gate evidence
- [x] Bounded recon-backed disjoint file ownership enumerated (T004/T012/T020/T021/T022/T023/T024)
- [x] `/speckit-clarify` coverage Clear across scope/domain/NFR/integration categories
- [x] `/speckit-checklist` requirements quality items above all pass
- [x] Proposed `constitution-amendment-v1.7.0.md` carries exact I/VII/VIII clauses, an **illustrative** Sync Impact Report summary (not verbatim staged artifact), and template-review expectations; prepared in this Phase A / spec PR; Phase B verify-only against Gate-1-approved staged constitution (not literal equality to the illustration); no constitution date edit (PC2)

## Domain checklist (quota-specific)

- [x] Shared per-database UTC daily budget matches Q1
- [x] `TryAcquireAsync` bool contract, atomic SQL, zero/one-row semantics, no refunds, retention match Q2 + Sentry M1
- [x] Handler after `AddStandardResilienceHandler`, ambient token, scope-per-attempt dispose-before-transport, two-signal mapping match Q3 + PC2
- [x] PC1 functional support names; ticket Core/table exact; signals one file each; no wider constitution exception
- [x] Both signals leave `MetadataProviderHealthState` unchanged; later interruption discards partials (Compass M1/M2)
- [x] Safe structured logs; no URI/key/raw exception leakage; existing movie id; no invented gate id (Sentry M4 / Ledger L4)
- [x] `Omdb:Quota` sealed record/`SectionName`/`[Range(1,int.MaxValue)]`, `omdb_daily_usage` shape, no limit CHECK, restart-only config match Q4/Q6 + Ledger L3
- [x] Exact positive counts: detail=1, search+detail=2, +1 per extra physical request (Compass L1)
- [x] Q5 ordered steps and PostgreSQL + shipped WireMock evidence encoded as mandatory; slow-gate budget bounds without commits=wire under timeout
- [x] I1: prerequisite fixture adaptations travel with the first step needing them on enumerated files; Api-host/matrix later with prerequisites for T023/T024
- [x] Q6 frozen scope, closing bar, round cap, Size M three-axis encoded; Q6 no-ADR wording superseded by C1/C2; I2 date-reconcile superseded by PC2
- [x] T034 Keel records checkbox + merge receipt in CONCLUSIONS; T035 verify-only against actual staged constitution (version/I/VII/VIII/its Sync Impact Report), not illustrative proposal summary; no constitution edit including dates; date-only mismatch recorded only; ADR Accepted delivery per CONCLUSIONS PC1/PC2 (implementation PR only)
- [x] Architecture whitelist follow-up for `IOmdbRequestGate` retained as Principle I companion edit
- [x] Constitution Check includes VII deterministic time, II/V internal-signal boundary, and IX (async testing) / Coding Conventions / PR quality gates with cites `:409-410`, `:475`, `:540-542` (Ledger L1/L2)
- [x] No new NuGet, public API, LocalPlay/secrets, cache, startup-log, or DEV-333 implementation claimed
- [x] Accepted denied-attempt DB cost and mismatched-host operational precondition carried without new features (Sentry M2/L2)

## Notes

- Clarify pass recorded in `spec.md` Clarifications — Gate 1 owner checkbox remains open; no other open drafting questions.
- Amendment timing: proposal + ADR-0031 Proposed prepared in this spec PR; PC2 removes Phase B constitution date-reconcile/apply and same-delivery ADR Accept implications from Quill artifacts.
- Quill drafted the amendment proposal record and cites Keel-owned ADR-0031; Quill does not edit constitution text or ADR bodies.
- Plan freeze and Gate 1 remain ungranted; Quill does not claim freeze or owner approval.
- PLAN_CHALLENGE ROUND 1 + ROUND 2 FINAL fix maps are in `DRAFTING_RECEIPT.md`. Unresolved R2 residuals: none.
- `/speckit-analyze` / plan-challenge adjudication not run by Quill (Keel owns).
