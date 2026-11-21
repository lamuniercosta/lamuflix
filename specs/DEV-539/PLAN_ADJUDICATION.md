# DEV-539 plan adjudication

**Status** — PLAN FROZEN on 2026-11-21 by Keel. Gate 1 awaits the owner-merged spec PR. Implementation, runtime acceptance, delivery review and merge clearance are pending.

**Evidence** — `findings-DEV-539-Sentry` (11), `findings-DEV-539-Ledger` (9), `findings-DEV-539-Compass` (5), `recon-DEV-539`, the [ticket](ticket-text.txt), [brief](brief.md), [Patron rulings](CONCLUSIONS.md), [spec](spec.md) and the constitution. Compass pins reviewed worktree HEAD `1bbc8a637b97ec2efdf0eaa6f2be6da8abbbc089`. The plans were working copies at challenge.

Each row is a disposition of the named finding. Accepted work is specified in the frozen plan and has a delivery task. Cross-axis duplicates share the same repair.

## Sentry — Risk

| ID | Severity | Disposition | Rationale and disposition in the plan |
| --- | --- | --- | --- |
| H1 | High | Accepted | Index TMDb IDs to sets of library movie IDs. Resolve across distinct TMDb identities, exclude self/equal-TMDb-ID edges, and link each eligible library row symmetrically. No uniqueness assumption or arbitrary row winner. Q2, FR-002/008/009. T012/T013 pin duplicate-source and duplicate-target behavior. |
| H2 | High | Partially accepted | Bound logical calls by eligible finds plus found matches (449 historical rows implies at most 898 logical calls at full eligibility and match rate). Process sequentially with cancellation, existing bounded transient retries (including 429), and shared breaker semantics. Abort with an explicit partial-run outcome if the breaker rejects calls. Committed rows are skipped on rerun. Additional throttling/partitioning and a persisted scan checkpoint are outside Q3/Q7 and FR-004. T009–T011, T019. |
| M1 | Medium | Accepted | Pin `--backfill-tmdb-recommendations`, strict operator invocation, configuration/migration preflight and nonzero refusal before host start or external work. T009/T011/T019, Q7 and FR-006/007. |
| M2 | Medium | Accepted | Report safe categories/counts; redact credential query values at the existing client logging boundary, omit full TMDb URIs/raw exception or response data from logs/output/evidence, and prove sentinel absence on success and failure. Constitution VI, Q3/Q7. T006/T008/T009/T011/T019. |
| M3 | Medium | Partially accepted | Register Crowd in the existing criterion and reason selection and prove a real Crowd win. Do not expand default strong-reason membership or bypass existing qualification rules; Q4 and SMOKE-001/002 condition the reason on existing selection. Validate the supported-name path where needed by the landed consumer. T015/T016. |
| M4 | Medium | Accepted | Introduce and test active-only optional threshold selection in T014–T016. T018 sets the value if DEV-545 supports it and verifies the final configuration with the same evidence. Inactive path uses the original bar. Q5, FR-015. |
| M5 | Medium | Partially accepted | Separate valid empty responses from transport or invalid payload failure and add sanitized recommendation-fetch outcome diagnostics. Preserve Q6/FR-005 ID-sensitive row lifecycle and existing keyword/credits health behavior. A scheduled reconciliation/TTL or change to provider-wide health policy is outside the approved enrichment scope. T006–T008. |
| M6 | Medium | Accepted | Capture or verify parity fixtures from a pinned pre-Crowd scorer revision, with harness/fixture provenance. Compare both consumers against that baseline, never generate the expected results using the changed scorer. Q4, FR-012, AC 6–7. T015a/T016. |
| L1 | Low | Accepted | Cascade source-owned pairs, retain incoming catalogue target IDs, index `recommended_tmdb_id` per Patron’s challenge ruling, and test bounded batched reads with no per-movie lookup. Q2, FR-008/009. T002/T003/T012/T013. |
| L2 | Low | Partially accepted | Assert one first-page request in the healthy multi-page fixture. Validate response shape and any echoed source ID when the response contract supplies one. Do not require a source-ID field on a paged list; result IDs identify targets. FR-004/005 and AC 3. T006/T008. |
| L3 | Low | Accepted | Pass the chosen seed title as data through existing reason composition and pin punctuation/braces handling and the exact ticket copy. Q4, SMOKE-001. T015/T016. |

## Ledger — Standards

| ID | Severity | Disposition | Rationale and disposition in the plan |
| --- | --- | --- | --- |
| H1 | High | Accepted | Name the scoring, reason, consumer and feature carrier boundaries. Pass immutable resolved links explicitly via the existing library feature snapshot, scoped to the operation; keep the scorer pure. Constitution I/II, Q2/Q4, ADR 0046. T001/T012/T015. |
| H2 | High | Accepted | Use a Core command, handler and result through existing ports and AddHandler. Api startup owns argument parsing, migration/configuration preflight, dispatch and output. Pickup verifies port fit; a genuinely new port requires a prior cited ruling and ADR under constitution I. Constitution I/II, Q7. T001/T009–T011. |
| M1 | Medium | Accepted | T014a aligns root glossary and current recommendation guidance with the conditional denominator, and ADR 0048 expressly amends the six-criterion scoring passage of ADR 0046. Keep historical artifacts as records. Constitution VIII, C1, challenge ruling. |
| M2 | Medium | Accepted | ADR 0048 records consequences and rejected sources/designs, including activation, persistence, backfill and the conditional denominator. Q1–Q7 and ticket Wanted 1. |
| M3 | Medium | Accepted | At pickup verify the evaluation loader/scorer and record DEV-545 readiness. T017 confirms candidate runs use the production Crowd feature/scoring path. If harness work is required, identify exact files and obtain a ruling before edits. Q5, FR-014/015. |
| M4 | Medium | Accepted | Name the threshold option, validation and existing consumer selection paths in T014–T016. T018 tunes only the effective active-state value and activation from evidence. Q5, ADR 0046 scoring invariants. |
| L1 | Low | Accepted | Validate Crowd weight with sibling range semantics, retain the independent review-channel behavior, and validate usable finite sums for effective modes without relying on Crowd weight to make inactive options valid. Separate activation Boolean. Q4/Q5, FR-011–013. T014/T016. |
| L2 | Low | Accepted | Use existing WireMock/Testcontainers helpers, injected TimeProvider if tracking timing, allowed comment conventions, and const static reason literals. Constitution IX and repo non-negotiables. T008/T011/T013/T015. |
| L3 | Low | Accepted | Centralize the glossary ruling and compress the constitution table. Confirm backup/rollback readiness before applying the home-library migration; attach only sanitized counts/evidence. Q3/Q8, constitution VI/VIII. T014a/T019. |

## Compass — Spec

| ID | Severity | Disposition | Rationale and disposition in the plan |
| --- | --- | --- | --- |
| F1 | Medium | Accepted | Pin the concrete scoring, mapper and carrier seams in the plan and verify at pickup. Same repair as Ledger H1. Q2/Q4, FR-008/011. T001/T012/T015. |
| F2 | Low | Accepted | Update weight-total validation and the fixed-six diagnostic for effective modes. Same repair as Ledger L1. FR-011/012. T014/T016. |
| F3 | Low | Accepted | Thread the winning seed title through the For you reason builder and explicitly map Crowd reason copy for Related. Q4, FR-011, SMOKE-001/002. T015/T016. |
| F4 | Low | Accepted | Apply deterministic set-based resolution for duplicate TMDb identities. Same repair as Sentry H1. FR-002/008/009. T012/T013. |
| F5 | Low | Accepted | Cascade deletion of source-owned pairs and retain other movies’ incoming catalogue IDs as unresolved targets. Same repair as the deletion portion of Sentry L1. Q2, FR-002/008/009. T002/T003/T013. |

**Totals** — 25/25 dispositions. 21 accepted, 4 partially accepted. No structural owner checkbox. The brief, plan, tasks and ADR carry the accepted changes. T000 controls Gate 1 and T001 verifies landed seams before implementation.
