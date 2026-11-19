# DEV-518 plan challenge adjudication

**Review inputs** — Maestri notes findings-DEV-518-Sentry (12), findings-DEV-518-Ledger (7), and findings-DEV-518-Compass (3), on feature/518-spec at ad5127ff with the draft plan and ADR. Decision basis includes ticket Wanted and Done when (recon-DEV-518 lines 32–60), spec FR-001–FR-014, and brief Q1–Q9.

| Finding | Severity | Disposition | Ruling and closure evidence |
|---|---|---|---|
| Sentry H-01 | High | Accepted | Validate a finite, positive sum of the six weights at startup. Explicit scorer callers must also reject invalid scoring inputs. T010 pins all-zero rejection and T008 pins valid configured denominator reconciliation. Q2 and FR-002. |
| Sentry H-02 | High | Accepted | Validate finite positive ThemesCeiling, finite ordered Plot bounds in the cosine domain, finite display thresholds in [0,1], and MinimumKeywordMovies ≥ 1. Normalize stoplist inputs per L-01. T009–T011 cover rejected configurations and valid boundaries. FR-005/006/010. |
| Sentry H-03 | High | Accepted | Read N and distinct n_k from one PostgreSQL snapshot and exercise a concurrent commit with Testcontainers. Empty libraries yield zero Themes. Scope the cache to the single catalog and serving process approved by Patron in CONCLUSIONS.md (d01e60f0) and documented in ADR 0046. Multiple populations and distributed invalidation are out of scope (noted, no ticket). T003/T006 and Q7. |
| Sentry H-04 | High | Accepted | Resolve one immutable snapshot at each operation boundary and explicitly thread the same value into every pair, including MMR and collection grouping where used. Patron approved the narrow For You handler change under Q6/Q7 in CONCLUSIONS.md (d01e60f0). T014 injects invalidation mid-operation and checks identity and next-operation refresh. FR-008/011 and Q7. |
| Sentry M-01 | Medium | Accepted | Record selection/order changes visible in the existing before/after receipts at T020 and exercise MMR/collection selection in T014. Q1 and Q6 keep current tuning in scope as a fixed input. Tuning changes require separate scope. |
| Sentry M-02 | Medium | Accepted | ADR 0046 now records numeric validation, consistent read, explicit reuse, deployment boundaries and alternatives. Q2/Q7 and earlier ADR 0045 style. |
| Sentry M-03 | Medium | Declined | Preserve ticket Wanted items 3 and 5 and US3 scenario 3. Evidence contains up to three shared counted names by rarity, including zero-weight names when they qualify. Zero score suppresses the reason under Q3/Q4. T009 pins ubiquitous-only evidence and the zero contribution. Dropping those names would change the specified evidence rule. |
| Sentry M-04 | Medium | Accepted | T001 inventories production writes affecting N, keyword membership, or eligibility. T004/T006 cover each applicable commit path and route gaps through existing scope rules. ADR 0046 records complete invalidation coverage as a dependency of the no-TTL design. FR-008/Q7. |
| Sentry M-05 | Medium | Partially accepted | T003/T006 inspect the representative PostgreSQL plan and existing index support, and test retry after failed load without caching failure. Q7 excludes schema changes, so a new index is outside the plan. Report any concrete query-cost gap at pickup. |
| Sentry L-01 | Low | Accepted | Compare using OrdinalIgnoreCase and trim, discard blank or null entries, and deduplicate. An explicitly null list is empty; the omitted setting keeps the four defaults. T009/T010 pin normalization and culture independence. Ticket Wanted item 3 and FR-005. |
| Sentry L-02 | Low | Accepted | T004/T006 require cancellation during stale-version retry and verify the cancelled load cannot populate the cache. Q7. |
| Sentry L-03 | Low | Confirmed existing | T014 restores a buildable revision before T019 records the candidate SHA and runs gates. T019 remains a prerequisite to after snapshots. Brief earlier analysis decision 1 and T010/T012/T014/T019. |
| Ledger 1 | Medium | Accepted | Keep the scorer pure with an explicit immutable keyword snapshot argument and cache access at operation boundaries. Plan keyword snapshot design and T011–T014. FR-008/011 and Q7. |
| Ledger 2 | Medium | Accepted | T001 enumerates applicable commit sites, T004 covers each site, and T006 adds success-only invalidation at each. Reuse existing architecture under Q7. A new shared invalidation abstraction is outside Q6/Q7 and remains a possible follow-up. |
| Ledger 3 | Medium | Accepted | Plan now cites ADR 0046 throughout and carries the adjudicated status. |
| Ledger 4 | Low | Accepted | ADR 0046 records the retired signals and rejected alternatives. Its decision text follows ADR 0045, which has no separate status field. |
| Ledger 5 | Low | Confirmed existing | 0046 was assigned by the Conductor and uses the established numeric-slug filename. Recon-DEV-518 lines 71–80 found 0045 highest on origin/main and the DEV-518 note records the claim. |
| Ledger 6 | Low | Accepted | Plan retains design and implementation dependencies. Drafting choreography remains in DRAFTING_RECEIPT.md. |
| Ledger 7 | Low | Accepted | Plan and T018 enumerate exact retired identifiers and phrases and define the Keyword option reference scan while allowing live keyword data and fixtures. SC-002. |
| Compass F1 | Medium | Accepted | ADR 0046 explicitly extends invalidation to successful keyword-membership changes even when genre invalidation is absent. FR-008 and US2 scenario 2. |
| Compass F2 | Low | Accepted | ADR 0046 names year, runtime, IMDb rating, writers, Identical feature profile, and the separate SimilarStoryThreshold reason path as retired. FR-004/006/007/010. |
| Compass F3 | Low | Accepted | Plan and tasks now reference ADR 0046. T000 carries the ADR and this adjudication in the spec PR. |

Sentry also noted that names in reasons and evaluation output are data. T016/T017 preserve literal metadata and use structured logging where applicable. The note did not assign this observation a finding ID, so it is outside the 22-row count.

**Totals** — 18 accepted, 1 partially accepted, 1 declined, and 2 confirmed existing. All four Sentry High findings are addressed in the design. The source note contains four High sections even though its opening verdict says three.

**Status** — PLAN FROZEN. All three reports are accounted for. Approved implementation boundaries, required tests, and task order are recorded in brief.md, plan.md, tasks.md, and ADR 0046. Gate 1 awaits the owner’s spec-PR merge. Implementation gates, DEV-545 receipts and home-library acceptance remain pending.
